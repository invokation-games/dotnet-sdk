using System.Collections.ObjectModel;
using Ivk.Skill.Sdk;
using Ivk.Skill.Sdk.Model;

namespace Ivk.Skill.Sdk.Tests.Net8;

/// <summary>
/// Smoke tests to verify the SDK works correctly on .NET 8.
/// </summary>
public class SmokeTests
{
    [Fact]
    public void CanCreateSdk()
    {
        using var sdk = SkillSdk.CreateBuilder()
            .WithApiKey("test-api-key")
            .Build();

        Assert.NotNull(sdk);
    }

    [Fact]
    public void CanCreateMatchResultRequest()
    {
        var request = new MatchResultRequest(
            teams: new Collection<TeamInfo>(),
            playerSessions: new Collection<PlayerSession>
            {
                new PlayerSession(playerId: "player_1", playerScore: 100)
                {
                    PriorMmr = 0.5,
                    PriorGamesPlayed = 10
                }
            })
        {
            MatchId = "test-match"
        };

        Assert.Equal("test-match", request.MatchId);
        Assert.Single(request.PlayerSessions);
    }

    [Fact]
    public void CanCreatePreMatchRequest()
    {
        var request = new PreMatchRequest(
            playerSessions: new Collection<PreMatchPlayerSession>
            {
                new PreMatchPlayerSession(playerId: "player_1")
                {
                    PriorMmr = 0.5,
                    TeamId = "blue"
                }
            },
            teams: new Collection<PreMatchTeamInfo>
            {
                new PreMatchTeamInfo(teamId: "blue")
            });

        Assert.Single(request.PlayerSessions);
        Assert.Single(request.Teams);
    }

    [Fact]
    public void CanConfigureRetry()
    {
        var config = new RetryConfig
        {
            MaxRetries = 5,
            InitialDelayMs = 100,
            MaxDelayMs = 5000
        };

        using var sdk = SkillSdk.CreateBuilder()
            .WithApiKey("test-api-key")
            .WithRetryConfig(config)
            .Build();

        Assert.NotNull(sdk);
    }

    [Fact]
    public void CanConfigureAllBuilderOptions()
    {
        using var httpClient = new HttpClient();

        using var sdk = SkillSdk.CreateBuilder()
            .WithApiKey("test-api-key")
            .WithBaseUrl("https://custom.api.com")
            .WithEnvironment("staging")
            .WithHttpClient(httpClient)
            .Build();

        Assert.NotNull(sdk);
    }
}
