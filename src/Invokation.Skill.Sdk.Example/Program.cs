using System.Collections.ObjectModel;
using Invokation.Skill.Sdk;
using Invokation.Skill.Sdk.Model;

Console.WriteLine("IVK Skill SDK Example");
Console.WriteLine("=====================");
Console.WriteLine();

// Get API key from environment variable
var apiKey = Environment.GetEnvironmentVariable("IVK_API_KEY");
var modelId = Environment.GetEnvironmentVariable("IVK_MODEL_ID") ?? "demo-model";

if (string.IsNullOrEmpty(apiKey))
{
    Console.WriteLine("Please set the IVK_API_KEY environment variable.");
    Console.WriteLine("Example: export IVK_API_KEY=your-api-key");
    Console.WriteLine();
    Console.WriteLine("Running in demo mode (API calls will fail without a valid key)...");
    apiKey = "demo-api-key";
}

// Create the SDK instance
using var sdk = SkillSdk.CreateBuilder()
    .WithApiKey(apiKey)
    .WithEnvironment("production")
    .WithRetryConfig(new RetryConfig
    {
        MaxRetries = 3,
        InitialDelayMs = 500,
        MaxDelayMs = 5000
    })
    .Build();

Console.WriteLine("SDK initialized successfully!");
Console.WriteLine();

// Example: Create a match result request for a 1v1 match
var matchResultRequest = new MatchResultRequest(
    teams: new Collection<TeamInfo>(),
    playerSessions: new Collection<PlayerSession>
    {
        new PlayerSession(
            playerId: "player_1",
            playerScore: 200
        )
        {
            PriorGamesPlayed = 80,
            PriorMmr = 0.5
        },
        new PlayerSession(
            playerId: "player_2",
            playerScore: 250
        )
        {
            PriorGamesPlayed = 70,
            PriorMmr = 0.4
        }
    }
)
{
    MatchId = "example-match-123"
};

Console.WriteLine("Example Match Result Request:");
Console.WriteLine($"  Match ID: {matchResultRequest.MatchId}");
Console.WriteLine($"  Players: {matchResultRequest.PlayerSessions.Count}");
foreach (var player in matchResultRequest.PlayerSessions)
{
    Console.WriteLine($"    - {player.PlayerId}: score={player.PlayerScore}, mmr={player.PriorMmr}");
}
Console.WriteLine();

// Example: Create a pre-match request for a 3v3 match
var preMatchRequest = new PreMatchRequest(
    playerSessions: new Collection<PreMatchPlayerSession>
    {
        new PreMatchPlayerSession(playerId: "player_1") { TeamId = "blue", PriorGamesPlayed = 100, PriorMmr = 0.6 },
        new PreMatchPlayerSession(playerId: "player_2") { TeamId = "blue", PriorGamesPlayed = 110, PriorMmr = 0.8 },
        new PreMatchPlayerSession(playerId: "player_3") { TeamId = "blue", PriorGamesPlayed = 180, PriorMmr = 0.7 },
        new PreMatchPlayerSession(playerId: "player_4") { TeamId = "red", PriorGamesPlayed = 150, PriorMmr = 0.7 },
        new PreMatchPlayerSession(playerId: "player_5") { TeamId = "red", PriorGamesPlayed = 110, PriorMmr = 0.6 },
        new PreMatchPlayerSession(playerId: "player_6") { TeamId = "red", PriorGamesPlayed = 160, PriorMmr = 0.9 }
    },
    teams: new Collection<PreMatchTeamInfo>
    {
        new PreMatchTeamInfo(teamId: "blue"),
        new PreMatchTeamInfo(teamId: "red")
    }
)
{
    MatchId = "example-prematch-456"
};

Console.WriteLine("Example Pre-Match Request:");
Console.WriteLine($"  Match ID: {preMatchRequest.MatchId}");
Console.WriteLine($"  Teams: {preMatchRequest.Teams.Count}");
Console.WriteLine($"  Players: {preMatchRequest.PlayerSessions.Count}");
Console.WriteLine();

// Uncomment to make actual API calls (requires valid API key and model ID)
/*
try
{
    Console.WriteLine("Calling PostMatchResultAsync...");
    var matchResult = await sdk.PostMatchResultAsync(modelId, matchResultRequest);
    Console.WriteLine($"Match processed successfully!");
    Console.WriteLine($"  Match duration: {matchResult.MatchInfo.Duration}");
    foreach (var player in matchResult.Players)
    {
        Console.WriteLine($"  {player.PlayerId}: {player.Prior.Mmr:F3} -> {player.Post.Mmr:F3}");
    }
}
catch (Exception ex)
{
    Console.WriteLine($"Error: {ex.Message}");
}

try
{
    Console.WriteLine("Calling PostPreMatchAsync...");
    var preMatchResult = await sdk.PostPreMatchAsync(modelId, preMatchRequest);
    Console.WriteLine($"Pre-match calculated successfully!");
    foreach (var team in preMatchResult.Teams)
    {
        Console.WriteLine($"  Team {team.Id}: expected={team.Expected:F3}");
    }
}
catch (Exception ex)
{
    Console.WriteLine($"Error: {ex.Message}");
}
*/

Console.WriteLine("Example completed!");
Console.WriteLine();
Console.WriteLine("To make actual API calls:");
Console.WriteLine("1. Set IVK_API_KEY environment variable");
Console.WriteLine("2. Set IVK_MODEL_ID environment variable");
Console.WriteLine("3. Uncomment the API call sections in the code");
