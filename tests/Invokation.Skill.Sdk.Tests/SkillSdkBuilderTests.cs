namespace Invokation.Skill.Sdk.Tests;

public class SkillSdkBuilderTests
{
    [Fact]
    public void Build_WithApiKey_CreatesSdk()
    {
        using var sdk = SkillSdk.CreateBuilder()
            .WithApiKey("test-api-key")
            .Build();

        Assert.NotNull(sdk);
    }

    [Fact]
    public void Build_WithoutApiKey_ThrowsInvalidOperationException()
    {
        var builder = SkillSdk.CreateBuilder();

        var exception = Assert.Throws<InvalidOperationException>(() => builder.Build());
        Assert.Contains("API key is required", exception.Message);
    }

    [Fact]
    public void Build_WithAllOptions_CreatesSdk()
    {
        using var httpClient = new HttpClient();

        using var sdk = SkillSdk.CreateBuilder()
            .WithApiKey("test-api-key")
            .WithBaseUrl("https://custom.api.com")
            .WithEnvironment("staging")
            .WithRetryConfig(new RetryConfig { MaxRetries = 5 })
            .WithHttpClient(httpClient)
            .Build();

        Assert.NotNull(sdk);
    }

    [Fact]
    public void WithApiKey_NullValue_ThrowsArgumentNullException()
    {
        var builder = SkillSdk.CreateBuilder();

        Assert.Throws<ArgumentNullException>(() => builder.WithApiKey(null!));
    }

    [Fact]
    public void WithBaseUrl_NullValue_ThrowsArgumentNullException()
    {
        var builder = SkillSdk.CreateBuilder();

        Assert.Throws<ArgumentNullException>(() => builder.WithBaseUrl(null!));
    }

    [Fact]
    public void WithEnvironment_NullValue_ThrowsArgumentNullException()
    {
        var builder = SkillSdk.CreateBuilder();

        Assert.Throws<ArgumentNullException>(() => builder.WithEnvironment(null!));
    }

    [Fact]
    public void WithRetryConfig_NullValue_ThrowsArgumentNullException()
    {
        var builder = SkillSdk.CreateBuilder();

        Assert.Throws<ArgumentNullException>(() => builder.WithRetryConfig(null!));
    }

    [Fact]
    public void WithHttpClient_NullValue_ThrowsArgumentNullException()
    {
        var builder = SkillSdk.CreateBuilder();

        Assert.Throws<ArgumentNullException>(() => builder.WithHttpClient(null!));
    }

    [Fact]
    public void Build_WithEmptyApiKey_ThrowsInvalidOperationException()
    {
        var builder = SkillSdk.CreateBuilder()
            .WithApiKey("   ");

        var exception = Assert.Throws<InvalidOperationException>(() => builder.Build());
        Assert.Contains("API key is required", exception.Message);
    }
}
