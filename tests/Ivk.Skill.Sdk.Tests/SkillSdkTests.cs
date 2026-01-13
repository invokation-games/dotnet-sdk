using System.Collections.ObjectModel;
using Ivk.Skill.Sdk.Model;

namespace Ivk.Skill.Sdk.Tests;

public class SkillSdkTests
{
    [Fact]
    public void PostMatchResult_NullModelId_ThrowsArgumentException()
    {
        using var sdk = CreateSdk();
        var request = new MatchResultRequest(
            teams: new Collection<TeamInfo>(),
            playerSessions: new Collection<PlayerSession>());

        Assert.Throws<ArgumentException>(() => sdk.PostMatchResult(null!, request));
    }

    [Fact]
    public void PostMatchResult_EmptyModelId_ThrowsArgumentException()
    {
        using var sdk = CreateSdk();
        var request = new MatchResultRequest(
            teams: new Collection<TeamInfo>(),
            playerSessions: new Collection<PlayerSession>());

        Assert.Throws<ArgumentException>(() => sdk.PostMatchResult("  ", request));
    }

    [Fact]
    public void PostMatchResult_NullRequest_ThrowsArgumentNullException()
    {
        using var sdk = CreateSdk();

        Assert.Throws<ArgumentNullException>(() => sdk.PostMatchResult("model-id", null!));
    }

    [Fact]
    public void PostPreMatch_NullModelId_ThrowsArgumentException()
    {
        using var sdk = CreateSdk();
        var request = new PreMatchRequest(
            playerSessions: new Collection<PreMatchPlayerSession>(),
            teams: new Collection<PreMatchTeamInfo>());

        Assert.Throws<ArgumentException>(() => sdk.PostPreMatch(null!, request));
    }

    [Fact]
    public void PostPreMatch_NullRequest_ThrowsArgumentNullException()
    {
        using var sdk = CreateSdk();

        Assert.Throws<ArgumentNullException>(() => sdk.PostPreMatch("model-id", null!));
    }

    [Fact]
    public void GetConfiguration_NullModelId_ThrowsArgumentException()
    {
        using var sdk = CreateSdk();

        Assert.Throws<ArgumentException>(() => sdk.GetConfiguration(null!));
    }

    [Fact]
    public async Task PostMatchResultAsync_NullModelId_ThrowsArgumentException()
    {
        using var sdk = CreateSdk();
        var request = new MatchResultRequest(
            teams: new Collection<TeamInfo>(),
            playerSessions: new Collection<PlayerSession>());

        await Assert.ThrowsAsync<ArgumentException>(
            () => sdk.PostMatchResultAsync(null!, request));
    }

    [Fact]
    public async Task PostMatchResultAsync_NullRequest_ThrowsArgumentNullException()
    {
        using var sdk = CreateSdk();

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => sdk.PostMatchResultAsync("model-id", null!));
    }

    [Fact]
    public async Task PostPreMatchAsync_NullModelId_ThrowsArgumentException()
    {
        using var sdk = CreateSdk();
        var request = new PreMatchRequest(
            playerSessions: new Collection<PreMatchPlayerSession>(),
            teams: new Collection<PreMatchTeamInfo>());

        await Assert.ThrowsAsync<ArgumentException>(
            () => sdk.PostPreMatchAsync(null!, request));
    }

    [Fact]
    public async Task PostPreMatchAsync_NullRequest_ThrowsArgumentNullException()
    {
        using var sdk = CreateSdk();

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => sdk.PostPreMatchAsync("model-id", null!));
    }

    [Fact]
    public async Task GetConfigurationAsync_NullModelId_ThrowsArgumentException()
    {
        using var sdk = CreateSdk();

        await Assert.ThrowsAsync<ArgumentException>(
            () => sdk.GetConfigurationAsync(null!));
    }

    [Fact]
    public void Dispose_CanBeCalledMultipleTimes()
    {
        var sdk = CreateSdk();

        sdk.Dispose();
        sdk.Dispose(); // Should not throw
    }

    [Fact]
    public async Task DisposeAsync_CanBeCalledMultipleTimes()
    {
        var sdk = CreateSdk();

        await sdk.DisposeAsync();
        await sdk.DisposeAsync(); // Should not throw
    }

    private static SkillSdk CreateSdk()
    {
        return SkillSdk.CreateBuilder()
            .WithApiKey("test-api-key")
            .Build();
    }
}
