namespace Invokation.Skill.Sdk.Tests;

public class RetryConfigTests
{
    [Fact]
    public void Default_ReturnsConfigWithDefaultValues()
    {
        var config = RetryConfig.Default;

        Assert.Equal(3, config.MaxRetries);
        Assert.Equal(500, config.InitialDelayMs);
        Assert.Equal(10000, config.MaxDelayMs);
    }

    [Fact]
    public void NoRetry_ReturnsConfigWithSingleRetry()
    {
        var config = RetryConfig.NoRetry;

        Assert.Equal(1, config.MaxRetries);
    }

    [Fact]
    public void CustomConfig_CanBeCreated()
    {
        var config = new RetryConfig
        {
            MaxRetries = 5,
            InitialDelayMs = 100,
            MaxDelayMs = 5000
        };

        Assert.Equal(5, config.MaxRetries);
        Assert.Equal(100, config.InitialDelayMs);
        Assert.Equal(5000, config.MaxDelayMs);
    }
}
