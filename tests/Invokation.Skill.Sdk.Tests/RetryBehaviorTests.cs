using System.Collections.ObjectModel;
using System.Net;
using System.Net.Http;
using System.Text;
using Invokation.Skill.Sdk.Client;
using Invokation.Skill.Sdk.Model;

namespace Invokation.Skill.Sdk.Tests;

public class RetryBehaviorTests
{
    [Fact]
    public async Task GetConfigurationAsync_Retries429WithFreshRequestAndSucceeds()
    {
        var handler = new RecordingHttpMessageHandler();
        handler.EnqueueResponse(CreateJsonResponse(HttpStatusCode.TooManyRequests, "{\"error\":\"slow down\"}"));
        handler.EnqueueResponse(CreateJsonResponse(HttpStatusCode.OK, CreateConfigurationResponseJson()));

        using var sdk = CreateSdk(handler, new RetryConfig
        {
            MaxRetries = 2,
            InitialDelayMs = 0,
            MaxDelayMs = 0
        });

        var response = await sdk.GetConfigurationAsync("model-id");

        Assert.Equal(2, handler.Requests.Count);
        Assert.NotSame(handler.Requests[0], handler.Requests[1]);
        Assert.Equal("cfg-1", response.Id);
        Assert.Equal(7, response.Revision);
    }

    [Fact]
    public async Task PostMatchResultAsync_Retries500AndResendsIdenticalJsonBody()
    {
        var handler = new RecordingHttpMessageHandler();
        handler.EnqueueResponse(CreateJsonResponse(HttpStatusCode.InternalServerError, "{\"error\":\"transient\"}"));
        handler.EnqueueResponse(CreateJsonResponse(HttpStatusCode.OK, CreateMatchResultResponseJson()));

        using var sdk = CreateSdk(handler, new RetryConfig
        {
            MaxRetries = 2,
            InitialDelayMs = 0,
            MaxDelayMs = 0
        });

        var response = await sdk.PostMatchResultAsync("model-id", CreateMatchResultRequest());

        Assert.Equal(2, handler.Requests.Count);
        Assert.NotSame(handler.Requests[0], handler.Requests[1]);
        Assert.Equal(handler.RequestBodies[0], handler.RequestBodies[1]);
        var player = Assert.Single(response.Players);
        Assert.Equal("player-1", player.PlayerId);
    }

    [Fact]
    public async Task GetConfigurationAsync_ExhaustedHttpRequestException_PreservesOriginalException()
    {
        var handler = new RecordingHttpMessageHandler();
        handler.EnqueueException(new HttpRequestException("boom line 1\nboom line 2"));
        handler.EnqueueException(new HttpRequestException("boom line 1\nboom line 2"));
        handler.EnqueueException(new HttpRequestException("boom line 1\nboom line 2"));

        using var sdk = CreateSdk(handler, new RetryConfig
        {
            MaxRetries = 3,
            InitialDelayMs = 0,
            MaxDelayMs = 0
        });

        var exception = await Assert.ThrowsAsync<HttpRequestException>(() => sdk.GetConfigurationAsync("model-id"));

        Assert.Contains("boom line 1", exception.Message);
        Assert.Contains("boom line 2", exception.Message);
        Assert.Equal(3, handler.Requests.Count);
    }

    [Fact]
    public async Task GetConfigurationAsync_Exhausted503_ThrowsApiExceptionAfterConfiguredAttempts()
    {
        var handler = new RecordingHttpMessageHandler();
        handler.EnqueueResponse(CreateJsonResponse(HttpStatusCode.ServiceUnavailable, "{\"error\":\"unavailable-1\"}"));
        handler.EnqueueResponse(CreateJsonResponse(HttpStatusCode.ServiceUnavailable, "{\"error\":\"unavailable-2\"}"));
        handler.EnqueueResponse(CreateJsonResponse(HttpStatusCode.ServiceUnavailable, "{\"error\":\"unavailable-3\"}"));

        using var sdk = CreateSdk(handler, new RetryConfig
        {
            MaxRetries = 3,
            InitialDelayMs = 0,
            MaxDelayMs = 0
        });

        var exception = await Assert.ThrowsAsync<ApiException>(() => sdk.GetConfigurationAsync("model-id"));

        Assert.Equal(503, exception.ErrorCode);
        Assert.Equal(3, handler.Requests.Count);
    }

    [Fact]
    public async Task GetConfigurationAsync_NoRetry_SendsOnlyOnce()
    {
        var handler = new RecordingHttpMessageHandler();
        handler.EnqueueResponse(CreateJsonResponse(HttpStatusCode.TooManyRequests, "{\"error\":\"slow down\"}"));

        using var sdk = CreateSdk(handler, RetryConfig.NoRetry);

        var exception = await Assert.ThrowsAsync<ApiException>(() => sdk.GetConfigurationAsync("model-id"));

        Assert.Equal(429, exception.ErrorCode);
        Assert.Single(handler.Requests);
    }

    [Theory]
    [InlineData(0, 0, 0, "MaxRetries")]
    [InlineData(1, -1, 0, "InitialDelayMs")]
    [InlineData(1, 0, -1, "MaxDelayMs")]
    public void Builder_WithInvalidRetryConfig_ThrowsArgumentOutOfRangeException(
        int maxRetries,
        int initialDelayMs,
        int maxDelayMs,
        string paramName)
    {
        var builder = SkillSdk.CreateBuilder();
        var config = new RetryConfig
        {
            MaxRetries = maxRetries,
            InitialDelayMs = initialDelayMs,
            MaxDelayMs = maxDelayMs
        };

        var exception = Assert.Throws<ArgumentOutOfRangeException>(() => builder.WithRetryConfig(config));

        Assert.Equal(paramName, exception.ParamName);
    }

    private static SkillSdk CreateSdk(RecordingHttpMessageHandler handler, RetryConfig retryConfig)
    {
        var httpClient = new HttpClient(handler);

        return SkillSdk.CreateBuilder()
            .WithApiKey("test-api-key")
            .WithHttpClient(httpClient)
            .WithRetryConfig(retryConfig)
            .Build();
    }

    private static HttpResponseMessage CreateJsonResponse(HttpStatusCode statusCode, string json)
    {
        return new HttpResponseMessage(statusCode)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
    }

    private static string CreateConfigurationResponseJson()
    {
        return "{\"id\":\"cfg-1\",\"model\":{},\"revision\":7}";
    }

    private static MatchResultRequest CreateMatchResultRequest()
    {
        return new MatchResultRequest(
            teams: new Collection<TeamInfo>
            {
                new TeamInfo(teamId: "blue", teamScore: 1)
            },
            playerSessions: new Collection<PlayerSession>
            {
                new PlayerSession(playerId: "player-1", playerScore: 1)
                {
                    TeamId = "blue"
                }
            });
    }

    private static string CreateMatchResultResponseJson()
    {
        var response = new MatchResultResponse(
            matchInfo: new MatchInfo(
                duration: 10,
                matchId: "match-1",
                maxTs: 10,
                meanMmr: 1000,
                minTs: 0,
                mmrDeviation: 0.1,
                partyCount: 0,
                playerCount: 1,
                teamCount: 1),
            players: new Collection<PlayerResult>
            {
                new PlayerResult(
                    extended: new PlayerUpdateExtended(
                        alpha: 1,
                        botLevel: 0,
                        isBot: false,
                        isFinalPlacement: false,
                        maxTs: 10,
                        minTs: 0,
                        mmrDelta: 5,
                        placementFrac: 1,
                        playerExpected: 0.5,
                        playerOutcome: 1,
                        playerScoreRate: 0.1,
                        playerWeight: 1,
                        residual: 0.5,
                        sessionCount: 1,
                        teamCount: 1,
                        teamExpected: 0.5,
                        teamOutcome: 1,
                        teamWeight: 1,
                        unifiedExpected: 0.5,
                        unifiedExpectedDist: new BetaDistribution(1, 1),
                        unifiedOutcome: 1),
                    playerId: "player-1",
                    playerIdx: 0,
                    post: new PriorPlayerStats(gamesPlayed: 11, mmr: 105, momentum: 0.6),
                    prior: new PriorPlayerStats(gamesPlayed: 10, mmr: 100, momentum: 0.5))
            },
            teams: new Collection<TeamResult>
            {
                new TeamResult(
                    beta: 1,
                    density: 1,
                    expected: 0.5,
                    id: "blue",
                    idx: 0,
                    mmr: 1000,
                    outcome: 1,
                    partyCount: 0,
                    score: 1,
                    size: 1)
            });

        return response.ToJson();
    }

    private sealed class RecordingHttpMessageHandler : HttpMessageHandler
    {
        private readonly Queue<Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>>> _responses = new();

        public List<HttpRequestMessage> Requests { get; } = new();
        public List<string?> RequestBodies { get; } = new();

        public void EnqueueResponse(HttpResponseMessage response)
        {
            _responses.Enqueue((request, _) =>
            {
                response.RequestMessage = request;
                return Task.FromResult(response);
            });
        }

        public void EnqueueException(Exception exception)
        {
            _responses.Enqueue((_, _) => Task.FromException<HttpResponseMessage>(exception));
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (_responses.Count == 0)
            {
                throw new InvalidOperationException("No scripted response was configured.");
            }

            Requests.Add(request);
            RequestBodies.Add(request.Content == null ? null : await request.Content.ReadAsStringAsync().ConfigureAwait(false));

            var next = _responses.Dequeue();
            return await next(request, cancellationToken).ConfigureAwait(false);
        }
    }
}
