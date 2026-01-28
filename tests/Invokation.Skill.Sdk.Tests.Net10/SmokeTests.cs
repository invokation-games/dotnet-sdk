using System.Collections.ObjectModel;
using Invokation.Skill.Sdk;
using Invokation.Skill.Sdk.Model;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Invokation.Skill.Sdk.Tests.Net10;

/// <summary>
/// Smoke tests to verify the SDK works correctly on .NET 10.
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

    #region Serialization Tests (EmitDefaultValue = false)

    /// <summary>
    /// Tests that required fields are always serialized.
    /// </summary>
    [Fact]
    public void PlayerSession_RequiredFields_AlwaysSerialized()
    {
        var session = new PlayerSession(playerId: "player_1", playerScore: 100);

        var json = session.ToJson();
        var jObject = JObject.Parse(json);

        // Required fields should always be present
        Assert.True(jObject.ContainsKey("player_id"), "player_id (required) should be serialized");
        Assert.True(jObject.ContainsKey("player_score"), "player_score (required) should be serialized");
        Assert.Equal("player_1", jObject["player_id"]!.Value<string>());
        Assert.Equal(100.0, jObject["player_score"]!.Value<double>());

        Console.WriteLine("=== Required Fields Test ===");
        Console.WriteLine(json);
    }

    /// <summary>
    /// Tests that optional nullable fields with null values are NOT serialized
    /// when EmitDefaultValue = false.
    /// </summary>
    [Fact]
    public void PlayerSession_OptionalNullableFields_NotSerializedWhenNull()
    {
        var session = new PlayerSession(playerId: "player_1", playerScore: 100);

        var json = session.ToJson();
        var jObject = JObject.Parse(json);

        // Optional nullable fields that are null should NOT be serialized
        Assert.False(jObject.ContainsKey("adjusted_mmr"), "adjusted_mmr should NOT be serialized when null");
        Assert.False(jObject.ContainsKey("bot_level"), "bot_level should NOT be serialized when null");
        Assert.False(jObject.ContainsKey("is_bot"), "is_bot should NOT be serialized when null");
        Assert.False(jObject.ContainsKey("party_id"), "party_id should NOT be serialized when null");
        Assert.False(jObject.ContainsKey("perf_beta"), "perf_beta should NOT be serialized when null");
        Assert.False(jObject.ContainsKey("player_score_start"), "player_score_start should NOT be serialized when null");
        Assert.False(jObject.ContainsKey("prior_mmr"), "prior_mmr should NOT be serialized when null");
        Assert.False(jObject.ContainsKey("prior_momentum"), "prior_momentum should NOT be serialized when null");
        Assert.False(jObject.ContainsKey("session_timestamps"), "session_timestamps should NOT be serialized when null");
        Assert.False(jObject.ContainsKey("team_id"), "team_id should NOT be serialized when null");
    }

    /// <summary>
    /// Tests that optional fields with non-null values ARE serialized.
    /// </summary>
    [Fact]
    public void PlayerSession_OptionalFields_SerializedWhenSet()
    {
        var session = new PlayerSession(
            playerId: "player_1",
            playerScore: 100,
            priorMmr: 0.5,
            teamId: "blue",
            isBot: false
        );

        var json = session.ToJson();
        var jObject = JObject.Parse(json);

        // Required fields
        Assert.True(jObject.ContainsKey("player_id"), "player_id should be serialized");
        Assert.True(jObject.ContainsKey("player_score"), "player_score should be serialized");

        // Optional fields with values should be serialized
        Assert.True(jObject.ContainsKey("prior_mmr"), "prior_mmr should be serialized when set");
        Assert.True(jObject.ContainsKey("team_id"), "team_id should be serialized when set");
        Assert.True(jObject.ContainsKey("is_bot"), "is_bot should be serialized when set (even if false)");

        // Verify values
        Assert.Equal(0.5, jObject["prior_mmr"]!.Value<double>());
        Assert.Equal("blue", jObject["team_id"]!.Value<string>());
        Assert.False(jObject["is_bot"]!.Value<bool>());

        // Fields NOT set should still be absent
        Assert.False(jObject.ContainsKey("adjusted_mmr"), "adjusted_mmr should NOT be serialized when not set");
        Assert.False(jObject.ContainsKey("bot_level"), "bot_level should NOT be serialized when not set");

        Console.WriteLine("=== Optional Fields Set Test ===");
        Console.WriteLine(json);
    }

    /// <summary>
    /// Tests that non-nullable value types with default value (0) are NOT serialized
    /// when EmitDefaultValue = false.
    /// </summary>
    [Fact]
    public void PlayerSession_DefaultValueTypes_NotSerializedWhenDefault()
    {
        // PriorGamesPlayed is a long (non-nullable) with default 0
        var session = new PlayerSession(playerId: "player_1", playerScore: 100, priorGamesPlayed: 0);

        var json = session.ToJson();
        var jObject = JObject.Parse(json);

        // prior_games_played = 0 should NOT be serialized (EmitDefaultValue = false)
        Assert.False(jObject.ContainsKey("prior_games_played"),
            "prior_games_played should NOT be serialized when 0 (default)");
    }

    /// <summary>
    /// Tests that non-nullable value types with non-default values ARE serialized.
    /// </summary>
    [Fact]
    public void PlayerSession_NonDefaultValueTypes_AreSerialized()
    {
        var session = new PlayerSession(playerId: "player_1", playerScore: 100, priorGamesPlayed: 50);

        var json = session.ToJson();
        var jObject = JObject.Parse(json);

        // prior_games_played = 50 should be serialized
        Assert.True(jObject.ContainsKey("prior_games_played"),
            "prior_games_played should be serialized when non-zero");
        Assert.Equal(50, jObject["prior_games_played"]!.Value<long>());
    }

    /// <summary>
    /// Tests a complete MatchResultRequest to ensure nested objects work correctly.
    /// </summary>
    [Fact]
    public void MatchResultRequest_NestedSerialization_WorksCorrectly()
    {
        var request = new MatchResultRequest(
            teams: new Collection<TeamInfo>
            {
                new TeamInfo(teamId: "blue", teamScore: 1),
                new TeamInfo(teamId: "red", teamScore: 0)
            },
            playerSessions: new Collection<PlayerSession>
            {
                new PlayerSession(playerId: "player_1", playerScore: 100, teamId: "blue"),
                new PlayerSession(playerId: "player_2", playerScore: 50, teamId: "red")
            }
        );

        var json = request.ToJson();
        var jObject = JObject.Parse(json);

        Console.WriteLine("=== MatchResultRequest Serialization ===");
        Console.WriteLine(json);

        // Required collection fields should be present
        Assert.True(jObject.ContainsKey("teams"), "teams should be serialized");
        Assert.True(jObject.ContainsKey("player_sessions"), "player_sessions should be serialized");

        // Optional fields on request should NOT be present
        Assert.False(jObject.ContainsKey("match_id"), "match_id should NOT be serialized when null");
        Assert.False(jObject.ContainsKey("match_start_ts"), "match_start_ts should NOT be serialized when null");
        Assert.False(jObject.ContainsKey("match_end_ts"), "match_end_ts should NOT be serialized when null");

        // Verify nested PlayerSession has required fields
        var playerSessions = jObject["player_sessions"] as JArray;
        Assert.NotNull(playerSessions);
        var firstPlayer = playerSessions[0] as JObject;
        Assert.NotNull(firstPlayer);
        Assert.True(firstPlayer.ContainsKey("player_id"), "Nested player_id should be serialized");
        Assert.True(firstPlayer.ContainsKey("player_score"), "Nested player_score should be serialized");
        Assert.True(firstPlayer.ContainsKey("team_id"), "Nested team_id should be serialized (was set)");
        Assert.False(firstPlayer.ContainsKey("prior_mmr"), "Nested prior_mmr should NOT be serialized (not set)");
    }

    /// <summary>
    /// Summary test showing the serialization behavior.
    /// </summary>
    [Fact]
    public void SerializationSummary_ShowsBehavior()
    {
        Console.WriteLine("=== SERIALIZATION BEHAVIOR SUMMARY (EmitDefaultValue = false) ===");
        Console.WriteLine();

        // Case 1: Minimal - only required fields
        var minimal = new PlayerSession(playerId: "player_1", playerScore: 100);
        Console.WriteLine("1. Minimal: new PlayerSession(playerId: \"player_1\", playerScore: 100)");
        Console.WriteLine($"   {minimal.ToJson().Replace("\n", "").Replace("  ", "")}");
        Console.WriteLine();

        // Case 2: With optional fields
        var withOptional = new PlayerSession(
            playerId: "player_1",
            playerScore: 100,
            priorMmr: 0.5,
            teamId: "blue"
        );
        Console.WriteLine("2. With optional: new PlayerSession(..., priorMmr: 0.5, teamId: \"blue\")");
        Console.WriteLine($"   {withOptional.ToJson().Replace("\n", "").Replace("  ", "")}");
        Console.WriteLine();

        // Case 3: With default value type (should be omitted)
        var withDefault = new PlayerSession(playerId: "player_1", playerScore: 100, priorGamesPlayed: 0);
        Console.WriteLine("3. With default: new PlayerSession(..., priorGamesPlayed: 0)");
        Console.WriteLine($"   {withDefault.ToJson().Replace("\n", "").Replace("  ", "")}");
        Console.WriteLine("   (priorGamesPlayed omitted because it's 0/default)");

        Assert.True(true);
    }

    #endregion
}
