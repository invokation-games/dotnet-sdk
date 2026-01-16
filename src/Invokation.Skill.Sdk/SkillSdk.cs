#nullable enable

using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Invokation.Skill.Sdk.Api;
using Invokation.Skill.Sdk.Client;
using Invokation.Skill.Sdk.Model;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Polly;

namespace Invokation.Skill.Sdk;

#if !NET8_0_OR_GREATER
internal static class ArgumentValidation
{
    public static void ThrowIfNullOrWhiteSpace(string? argument, string? paramName = null)
    {
        if (argument is null)
        {
            throw new ArgumentNullException(paramName);
        }

        if (string.IsNullOrWhiteSpace(argument))
        {
            throw new ArgumentException("Value cannot be null or whitespace.", paramName);
        }
    }
}
#endif

internal static class Log
{
    private static readonly Action<ILogger, int, int, double, string, Exception?> RetryAttemptAction =
        LoggerMessage.Define<int, int, double, string>(
            LogLevel.Warning,
            new EventId(1, nameof(RetryAttempt)),
            "Retry attempt {Attempt}/{Max} after {Delay}ms: {Message}");

    public static void RetryAttempt(ILogger logger, int attempt, int maxRetries, double delayMs, string message)
    {
        RetryAttemptAction(logger, attempt, maxRetries, delayMs, message, null);
    }
}

/// <summary>
/// SDK wrapper for the IVK Skill API with built-in retry mechanism and API key authentication.
///
/// This SDK provides both synchronous and asynchronous APIs for skill rating updates and match predictions.
///
/// Example usage:
/// <code>
/// var sdk = SkillSdk.CreateBuilder()
///     .WithApiKey("your-api-key")
///     .WithEnvironment("production")
///     .Build();
///
/// var result = await sdk.PostMatchResultAsync("model-id", matchResultRequest);
/// </code>
/// </summary>
public sealed class SkillSdk : IDisposable, IAsyncDisposable
{
    private readonly SkillApi _skillApi;
    private readonly string _environment;
    private readonly ILogger<SkillSdk> _logger;
    private readonly bool _ownsHttpClient;
    private readonly HttpClient _httpClient;
    private bool _disposed;

    private SkillSdk(
        string apiKey,
        string baseUrl,
        string environment,
        RetryConfig retryConfig,
        HttpClient httpClient,
        bool ownsHttpClient,
        ILogger<SkillSdk>? logger)
    {
        _environment = environment ?? throw new ArgumentNullException(nameof(environment));
        _logger = logger ?? NullLogger<SkillSdk>.Instance;
        _ownsHttpClient = ownsHttpClient;
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));

        // Configure the API client with the API key
        var configuration = new Configuration
        {
            BasePath = baseUrl
        };
        configuration.ApiKey["x-ivk-apikey"] = apiKey;

        _skillApi = new SkillApi(httpClient, configuration);

        // Configure the global retry policy using the generated RetryConfiguration
        ConfigureRetryPolicy(retryConfig ?? RetryConfig.Default);
    }

    private void ConfigureRetryPolicy(RetryConfig config)
    {
        RetryConfiguration.AsyncRetryPolicy = Policy
            .HandleResult<HttpResponseMessage>(r =>
                r.StatusCode == HttpStatusCode.TooManyRequests ||
                r.StatusCode == HttpStatusCode.ServiceUnavailable ||
                r.StatusCode == HttpStatusCode.GatewayTimeout ||
                r.StatusCode == HttpStatusCode.BadGateway ||
                (int)r.StatusCode >= 500)
            .Or<HttpRequestException>()
            .Or<TaskCanceledException>(ex => !ex.CancellationToken.IsCancellationRequested)
            .WaitAndRetryAsync(
                retryCount: config.MaxRetries - 1,
                sleepDurationProvider: attempt => CalculateBackoffDelay(attempt, config),
                onRetry: (outcome, delay, attempt, _) =>
                {
                    var message = outcome.Exception?.Message ?? $"HTTP {(int?)outcome.Result?.StatusCode}";
                    Log.RetryAttempt(_logger, attempt, config.MaxRetries - 1, delay.TotalMilliseconds, message);
                });

        RetryConfiguration.RetryPolicy = Policy
            .HandleResult<HttpResponseMessage>(r =>
                r.StatusCode == HttpStatusCode.TooManyRequests ||
                r.StatusCode == HttpStatusCode.ServiceUnavailable ||
                r.StatusCode == HttpStatusCode.GatewayTimeout ||
                r.StatusCode == HttpStatusCode.BadGateway ||
                (int)r.StatusCode >= 500)
            .Or<HttpRequestException>()
            .WaitAndRetry(
                retryCount: config.MaxRetries - 1,
                sleepDurationProvider: attempt => CalculateBackoffDelay(attempt, config),
                onRetry: (outcome, delay, attempt, _) =>
                {
                    var message = outcome.Exception?.Message ?? $"HTTP {(int?)outcome.Result?.StatusCode}";
                    Log.RetryAttempt(_logger, attempt, config.MaxRetries - 1, delay.TotalMilliseconds, message);
                });
    }

    private static TimeSpan CalculateBackoffDelay(int attempt, RetryConfig config)
    {
        var exponentialDelay = config.InitialDelayMs * Math.Pow(2, attempt - 1);
        var clampedDelay = Math.Min(exponentialDelay, config.MaxDelayMs);
        return TimeSpan.FromMilliseconds(clampedDelay);
    }

    // ===== Async API (Task-based) =====

    /// <summary>
    /// Submit match results to update player skill ratings asynchronously.
    /// </summary>
    /// <param name="modelId">The model ID to use for skill calculations.</param>
    /// <param name="request">The match result data containing player sessions and team information.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>MatchResultResponse containing updated skill ratings for each player.</returns>
    /// <exception cref="ApiException">Thrown when the API returns an error response.</exception>
    /// <exception cref="HttpRequestException">Thrown when a network error occurs after all retries.</exception>
    public async Task<MatchResultResponse> PostMatchResultAsync(
        string modelId,
        MatchResultRequest request,
        CancellationToken cancellationToken = default)
    {
        ThrowIfNullOrWhiteSpace(modelId);
        ArgumentNullException.ThrowIfNull(request);

        return await _skillApi.PostMatchResultAsync(modelId, _environment, request, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Calculate expected match outcomes before the match starts asynchronously.
    /// </summary>
    /// <param name="modelId">The model ID to use for predictions.</param>
    /// <param name="request">The pre-match data with player information.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>PreMatchResponse containing expected outcomes for each player and team.</returns>
    /// <exception cref="ApiException">Thrown when the API returns an error response.</exception>
    /// <exception cref="HttpRequestException">Thrown when a network error occurs after all retries.</exception>
    public async Task<PreMatchResponse> PostPreMatchAsync(
        string modelId,
        PreMatchRequest request,
        CancellationToken cancellationToken = default)
    {
        ThrowIfNullOrWhiteSpace(modelId);
        ArgumentNullException.ThrowIfNull(request);

        return await _skillApi.PostPreMatchAsync(modelId, _environment, request, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Get the current active model configuration asynchronously.
    /// </summary>
    /// <param name="modelId">The model ID to retrieve configuration for.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>ConfigurationResponse containing the model configuration.</returns>
    /// <exception cref="ApiException">Thrown when the API returns an error response.</exception>
    /// <exception cref="HttpRequestException">Thrown when a network error occurs after all retries.</exception>
    public async Task<ConfigurationResponse> GetConfigurationAsync(
        string modelId,
        CancellationToken cancellationToken = default)
    {
        ThrowIfNullOrWhiteSpace(modelId);

        return await _skillApi.GetConfigurationAsync(modelId, cancellationToken)
            .ConfigureAwait(false);
    }

    // ===== Sync API (Blocking) =====

    /// <summary>
    /// Submit match results to update player skill ratings synchronously.
    /// </summary>
    /// <param name="modelId">The model ID to use for skill calculations.</param>
    /// <param name="request">The match result data containing player sessions and team information.</param>
    /// <returns>MatchResultResponse containing updated skill ratings for each player.</returns>
    /// <exception cref="ApiException">Thrown when the API returns an error response.</exception>
    /// <exception cref="HttpRequestException">Thrown when a network error occurs after all retries.</exception>
    public MatchResultResponse PostMatchResult(string modelId, MatchResultRequest request)
    {
        ThrowIfNullOrWhiteSpace(modelId);
        ArgumentNullException.ThrowIfNull(request);

        return _skillApi.PostMatchResult(modelId, _environment, request);
    }

    /// <summary>
    /// Calculate expected match outcomes before the match starts synchronously.
    /// </summary>
    /// <param name="modelId">The model ID to use for predictions.</param>
    /// <param name="request">The pre-match data with player information.</param>
    /// <returns>PreMatchResponse containing expected outcomes for each player and team.</returns>
    /// <exception cref="ApiException">Thrown when the API returns an error response.</exception>
    /// <exception cref="HttpRequestException">Thrown when a network error occurs after all retries.</exception>
    public PreMatchResponse PostPreMatch(string modelId, PreMatchRequest request)
    {
        ThrowIfNullOrWhiteSpace(modelId);
        ArgumentNullException.ThrowIfNull(request);

        return _skillApi.PostPreMatch(modelId, _environment, request);
    }

    /// <summary>
    /// Get the current active model configuration synchronously.
    /// </summary>
    /// <param name="modelId">The model ID to retrieve configuration for.</param>
    /// <returns>ConfigurationResponse containing the model configuration.</returns>
    /// <exception cref="ApiException">Thrown when the API returns an error response.</exception>
    /// <exception cref="HttpRequestException">Thrown when a network error occurs after all retries.</exception>
    public ConfigurationResponse GetConfiguration(string modelId)
    {
        ThrowIfNullOrWhiteSpace(modelId);
        return _skillApi.GetConfiguration(modelId);
    }

    private static void ThrowIfNullOrWhiteSpace(string? argument, [System.Runtime.CompilerServices.CallerArgumentExpression(nameof(argument))] string? paramName = null)
    {
#if NET8_0_OR_GREATER
        ArgumentException.ThrowIfNullOrWhiteSpace(argument, paramName);
#else
        ArgumentValidation.ThrowIfNullOrWhiteSpace(argument, paramName);
#endif
    }

    // ===== Builder =====

    /// <summary>
    /// Creates a new builder for constructing SkillSdk instances.
    /// </summary>
    /// <returns>A new builder instance.</returns>
    public static Builder CreateBuilder() => new();

    /// <summary>
    /// Builder class for constructing SkillSdk instances with a fluent API.
    /// </summary>
    public sealed class Builder
    {
        private string? _apiKey;
        private string _baseUrl = "https://skill.ivk.dev";
        private string _environment = "production";
        private RetryConfig _retryConfig = RetryConfig.Default;
        private HttpClient? _httpClient;
        private ILogger<SkillSdk>? _logger;

        internal Builder() { }

        /// <summary>
        /// Set the API key for authentication (required).
        /// </summary>
        /// <param name="apiKey">The API key obtained from the IVK dashboard.</param>
        /// <returns>This builder instance for chaining.</returns>
        public Builder WithApiKey(string apiKey)
        {
            _apiKey = apiKey ?? throw new ArgumentNullException(nameof(apiKey));
            return this;
        }

        /// <summary>
        /// Set the base URL for the API (optional, defaults to https://skill.ivk.dev).
        /// </summary>
        /// <param name="baseUrl">The base URL of the API.</param>
        /// <returns>This builder instance for chaining.</returns>
        public Builder WithBaseUrl(string baseUrl)
        {
            _baseUrl = baseUrl ?? throw new ArgumentNullException(nameof(baseUrl));
            return this;
        }

        /// <summary>
        /// Set the environment (optional, defaults to "production").
        /// </summary>
        /// <param name="environment">The environment name (e.g., "production", "staging").</param>
        /// <returns>This builder instance for chaining.</returns>
        public Builder WithEnvironment(string environment)
        {
            _environment = environment ?? throw new ArgumentNullException(nameof(environment));
            return this;
        }

        /// <summary>
        /// Set custom retry configuration (optional).
        /// </summary>
        /// <param name="config">The retry configuration.</param>
        /// <returns>This builder instance for chaining.</returns>
        public Builder WithRetryConfig(RetryConfig config)
        {
            _retryConfig = config ?? throw new ArgumentNullException(nameof(config));
            return this;
        }

        /// <summary>
        /// Set a custom HttpClient (optional).
        /// When provided, the SDK will not dispose of this client.
        /// </summary>
        /// <remarks>
        /// For optimal performance, configure your HttpClient with:
        /// <list type="bullet">
        ///   <item><description>HTTP/2 support: Set <c>DefaultRequestVersion = HttpVersion.Version20</c> and <c>DefaultVersionPolicy = HttpVersionPolicy.RequestVersionOrLower</c></description></item>
        ///   <item><description>Connection idle timeout: Use <see cref="SocketsHttpHandler"/> with <c>PooledConnectionIdleTimeout = TimeSpan.FromSeconds(50)</c> to prevent connection resets (the API server timeout is 60s)</description></item>
        /// </list>
        /// </remarks>
        /// <param name="httpClient">The HttpClient to use for API calls.</param>
        /// <returns>This builder instance for chaining.</returns>
        public Builder WithHttpClient(HttpClient httpClient)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            return this;
        }

        /// <summary>
        /// Set a logger for the SDK (optional).
        /// </summary>
        /// <param name="logger">The logger instance.</param>
        /// <returns>This builder instance for chaining.</returns>
        public Builder WithLogger(ILogger<SkillSdk> logger)
        {
            _logger = logger;
            return this;
        }

        /// <summary>
        /// Build the SkillSdk instance.
        /// </summary>
        /// <returns>A configured SkillSdk instance.</returns>
        /// <exception cref="InvalidOperationException">If required parameters are missing.</exception>
        public SkillSdk Build()
        {
            if (string.IsNullOrWhiteSpace(_apiKey))
                throw new InvalidOperationException("API key is required. Call WithApiKey() before Build().");

            if (string.IsNullOrWhiteSpace(_baseUrl))
                throw new InvalidOperationException("Base URL cannot be empty.");

            if (string.IsNullOrWhiteSpace(_environment))
                throw new InvalidOperationException("Environment cannot be empty.");

            var ownsHttpClient = _httpClient == null;
            var httpClient = _httpClient ?? CreateDefaultHttpClient();

            return new SkillSdk(
                _apiKey,
                _baseUrl,
                _environment,
                _retryConfig,
                httpClient,
                ownsHttpClient,
                _logger);
        }

        private static HttpClient CreateDefaultHttpClient()
        {
            var handler = new SocketsHttpHandler
            {
                PooledConnectionIdleTimeout = TimeSpan.FromSeconds(50)
            };

            return new HttpClient(handler, disposeHandler: true)
            {
                DefaultRequestVersion = HttpVersion.Version20,
                DefaultVersionPolicy = HttpVersionPolicy.RequestVersionOrLower
            };
        }
    }

    // ===== Disposal =====

    /// <summary>
    /// Disposes the SDK and releases resources.
    /// </summary>
    public void Dispose()
    {
        if (_disposed) return;

        _skillApi?.Dispose();
        if (_ownsHttpClient)
        {
            _httpClient?.Dispose();
        }
        _disposed = true;
    }

    /// <summary>
    /// Asynchronously disposes the SDK and releases resources.
    /// </summary>
    public ValueTask DisposeAsync()
    {
        Dispose();
        return ValueTask.CompletedTask;
    }
}
