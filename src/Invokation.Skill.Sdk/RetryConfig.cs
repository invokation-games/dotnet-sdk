using System;

namespace Invokation.Skill.Sdk;

/// <summary>
/// Configuration for retry behavior with exponential backoff.
/// </summary>
public sealed class RetryConfig
{
    /// <summary>
    /// Maximum number of retry attempts.
    /// </summary>
    public int MaxRetries { get; init; } = 3;

    /// <summary>
    /// Initial delay in milliseconds before first retry.
    /// </summary>
    public int InitialDelayMs { get; init; } = 500;

    /// <summary>
    /// Maximum delay in milliseconds between retries.
    /// </summary>
    public int MaxDelayMs { get; init; } = 10000;

    /// <summary>
    /// Gets the default retry configuration.
    /// </summary>
    public static RetryConfig Default => new();

    /// <summary>
    /// Creates a new retry configuration with no retries (fail immediately).
    /// </summary>
    public static RetryConfig NoRetry => new() { MaxRetries = 1 };
}

internal static class RetryConfigValidator
{
    public static void Validate(RetryConfig config)
    {
        ArgumentNullException.ThrowIfNull(config);

        if (config.MaxRetries < 1)
        {
            throw new ArgumentOutOfRangeException(
                nameof(config.MaxRetries),
                config.MaxRetries,
                "RetryConfig.MaxRetries must be greater than or equal to 1.");
        }

        if (config.InitialDelayMs < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(config.InitialDelayMs),
                config.InitialDelayMs,
                "RetryConfig.InitialDelayMs must be greater than or equal to 0.");
        }

        if (config.MaxDelayMs < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(config.MaxDelayMs),
                config.MaxDelayMs,
                "RetryConfig.MaxDelayMs must be greater than or equal to 0.");
        }
    }
}
