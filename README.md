# IVK Skill SDK for C#

Official C# SDK for the [Invokation Skill API](https://docs.ivk.dev) - skill ratings and matchmaking for games.

## Installation

Install via NuGet:

```bash
dotnet add package Ivk.Skill.Sdk
```

Or via the Package Manager Console:

```powershell
Install-Package Ivk.Skill.Sdk
```

## Quick Start

```csharp
using Ivk.Skill.Sdk;
using Ivk.Skill.Sdk.Model;
using System.Collections.ObjectModel;

// Create the SDK instance
using var sdk = SkillSdk.CreateBuilder()
    .WithApiKey("your-api-key")
    .WithEnvironment("production")
    .Build();

// Submit match results
var ratingUpdates = await sdk.PostMatchResultAsync("your-model-id", new MatchResultRequest(
    teams: new Collection<TeamInfo>(),
    playerSessions: new Collection<PlayerSession>
    {
        new PlayerSession(playerId: "player_1", playerScore: 200)
        {
            PriorGamesPlayed = 80,
            PriorMmr = 0.5
        },
        new PlayerSession(playerId: "player_2", playerScore: 250)
        {
            PriorGamesPlayed = 70,
            PriorMmr = 0.4
        }
    }
)
{
    MatchId = "unique-match-id"
});

// Access updated ratings
foreach (var player in ratingUpdates.Players)
{
    Console.WriteLine($"{player.PlayerId}: {player.Prior.Mmr:F3} -> {player.Post.Mmr:F3}");
}
```

## Features

- **Async/await and sync APIs** - Choose between `PostMatchResultAsync()` or `PostMatchResult()`
- **Automatic retry with exponential backoff** - Configurable retry behavior for transient failures
- **API key authentication** - Simple setup via the builder pattern
- **Full .NET support** - Targets .NET 6, .NET 8, .NET 10, and .NET Standard 2.1

## Configuration

### Builder Options

```csharp
var sdk = SkillSdk.CreateBuilder()
    .WithApiKey("your-api-key")           // Required: API key from IVK dashboard
    .WithBaseUrl("https://skill.ivk.dev") // Optional: API base URL (default shown)
    .WithEnvironment("production")         // Optional: Environment name (default: "production")
    .WithRetryConfig(new RetryConfig       // Optional: Retry configuration
    {
        MaxRetries = 3,
        InitialDelayMs = 500,
        MaxDelayMs = 10000
    })
    .WithHttpClient(httpClient)            // Optional: Custom HttpClient
    .WithLogger(logger)                    // Optional: ILogger for debugging
    .Build();
```

### Retry Configuration

The SDK automatically retries failed requests with exponential backoff:

```csharp
var retryConfig = new RetryConfig
{
    MaxRetries = 5,        // Total attempts (default: 3)
    InitialDelayMs = 100,  // First retry delay (default: 500ms)
    MaxDelayMs = 30000     // Maximum delay cap (default: 10000ms)
};
```

To disable retries:

```csharp
.WithRetryConfig(RetryConfig.NoRetry)
```

## API Reference

### PostMatchResultAsync / PostMatchResult

Submit match results to update player skill ratings.

```csharp
var result = await sdk.PostMatchResultAsync(
    modelId: "your-model-id",
    request: matchResultRequest,
    cancellationToken: token  // Optional
);
```

### PostPreMatchAsync / PostPreMatch

Calculate expected match outcomes before a match starts.

```csharp
var prediction = await sdk.PostPreMatchAsync(
    modelId: "your-model-id",
    request: preMatchRequest
);

foreach (var team in prediction.Teams)
{
    Console.WriteLine($"Team {team.Id}: {team.Expected:P1} win probability");
}
```

### GetConfigurationAsync / GetConfiguration

Retrieve the current model configuration.

```csharp
var config = await sdk.GetConfigurationAsync(modelId: "your-model-id");
Console.WriteLine($"Model revision: {config.Revision}");
```

## Examples

### 1v1 Match

```csharp
var request = new MatchResultRequest(
    teams: new Collection<TeamInfo>(),
    playerSessions: new Collection<PlayerSession>
    {
        new PlayerSession(playerId: "winner", playerScore: 100) { PriorMmr = 0.5 },
        new PlayerSession(playerId: "loser", playerScore: 50) { PriorMmr = 0.5 }
    }
);

var result = await sdk.PostMatchResultAsync("model-id", request);
```

### Team Match (3v3)

```csharp
var request = new MatchResultRequest(
    teams: new Collection<TeamInfo>
    {
        new TeamInfo(teamId: "blue", teamScore: 3),
        new TeamInfo(teamId: "red", teamScore: 1)
    },
    playerSessions: new Collection<PlayerSession>
    {
        // Blue team
        new PlayerSession(playerId: "p1", playerScore: 100) { TeamId = "blue", PriorMmr = 0.6 },
        new PlayerSession(playerId: "p2", playerScore: 80) { TeamId = "blue", PriorMmr = 0.5 },
        new PlayerSession(playerId: "p3", playerScore: 90) { TeamId = "blue", PriorMmr = 0.55 },
        // Red team
        new PlayerSession(playerId: "p4", playerScore: 70) { TeamId = "red", PriorMmr = 0.5 },
        new PlayerSession(playerId: "p5", playerScore: 60) { TeamId = "red", PriorMmr = 0.45 },
        new PlayerSession(playerId: "p6", playerScore: 50) { TeamId = "red", PriorMmr = 0.4 }
    }
);

var result = await sdk.PostMatchResultAsync("model-id", request);
```

## Error Handling

The SDK throws specific exceptions for different error conditions:

```csharp
try
{
    var result = await sdk.PostMatchResultAsync(modelId, request);
}
catch (ApiException ex) when (ex.ErrorCode == 401)
{
    Console.WriteLine("Invalid API key");
}
catch (ApiException ex) when (ex.ErrorCode == 404)
{
    Console.WriteLine("Model not found");
}
catch (HttpRequestException ex)
{
    Console.WriteLine($"Network error: {ex.Message}");
}
```

## Dependency Injection

For ASP.NET Core applications:

```csharp
// In Program.cs or Startup.cs
services.AddSingleton<SkillSdk>(sp =>
{
    var logger = sp.GetService<ILogger<SkillSdk>>();
    return SkillSdk.CreateBuilder()
        .WithApiKey(Configuration["IvkApiKey"])
        .WithLogger(logger)
        .Build();
});
```

## Development

### Prerequisites

- .NET 6.0 SDK or later
- Docker (for OpenAPI code generation)
- Just (task runner)

### Common Tasks

```bash
# Build
just build

# Run tests
just test

# Generate SDK from OpenAPI spec
just generate-sdk

# Pack for NuGet
just pack

# Clean artifacts
just clean
```

## Support

- Documentation: [https://docs.ivk.dev](https://docs.ivk.dev)
- Discord: [Community Discord](https://discord.gg/JfNGsunrjX)
