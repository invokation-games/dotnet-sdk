using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Ivk.Skill.Sdk.Api;
using Ivk.Skill.Sdk.Client;
using Ivk.Skill.Sdk.Model;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Polly;
using Polly.Retry;

namespace Ivk.Skill.Sdk;

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
    private readonly RetryConfig _retryConfig;
    private readonly ILogger<SkillSdk> _logger;
    private readonly AsyncRetryPolicy _asyncRetryPolicy;
    private readonly RetryPolicy _syncRetryPolicy;
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
        ILogger<SkillSdk> logger)
    {
        _environment = environment ?? throw new ArgumentNullException(nameof(environment));
        _retryConfig = retryConfig ?? throw new ArgumentNullException(nameof(retryConfig));
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

        // Build retry policies
        _asyncRetryPolicy = Policy
            .Handle<HttpRequestException>()
            .Or<TaskCanceledException>(ex => !ex.CancellationToken.IsCancellationRequested)
            .Or<ApiException>(ex => ex.ErrorCode >= 500)
            .WaitAndRetryAsync(
                retryCount: _retryConfig.MaxRetries - 1,
                sleepDurationProvider: attempt => CalculateBackoffDelay(attempt),
                onRetry: (exception, delay, attempt, _) =>
                    _logger.LogWarning(exception,
                        "Retry attempt {Attempt}/{Max} after {Delay}ms",
                        attempt, _retryConfig.MaxRetries - 1, delay.TotalMilliseconds));

        _syncRetryPolicy = Policy
            .Handle<HttpRequestException>()
            .Or<ApiException>(ex => ex.ErrorCode >= 500)
            .WaitAndRetry(
                retryCount: _retryConfig.MaxRetries - 1,
                sleepDurationProvider: attempt => CalculateBackoffDelay(attempt),
                onRetry: (exception, delay, attempt, _) =>
                    _logger.LogWarning(exception,
                        "Retry attempt {Attempt}/{Max} after {Delay}ms",
                        attempt, _retryConfig.MaxRetries - 1, delay.TotalMilliseconds));
    }

    private TimeSpan CalculateBackoffDelay(int attempt)
    {
        var exponentialDelay = _retryConfig.InitialDelayMs * Math.Pow(2, attempt - 1);
        var clampedDelay = Math.Min(exponentialDelay, _retryConfig.MaxDelayMs);
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
        if (string.IsNullOrWhiteSpace(modelId))
            throw new ArgumentException("Model ID cannot be null or empty", nameof(modelId));
        if (request == null)
            throw new ArgumentNullException(nameof(request));

        return await _asyncRetryPolicy.ExecuteAsync(async () =>
            await _skillApi.PostMatchResultAsync(modelId, _environment, request, cancellationToken)
                .ConfigureAwait(false)).ConfigureAwait(false);
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
        if (string.IsNullOrWhiteSpace(modelId))
            throw new ArgumentException("Model ID cannot be null or empty", nameof(modelId));
        if (request == null)
            throw new ArgumentNullException(nameof(request));

        return await _asyncRetryPolicy.ExecuteAsync(async () =>
            await _skillApi.PostPreMatchAsync(modelId, _environment, request, cancellationToken)
                .ConfigureAwait(false)).ConfigureAwait(false);
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
        if (string.IsNullOrWhiteSpace(modelId))
            throw new ArgumentException("Model ID cannot be null or empty", nameof(modelId));

        return await _asyncRetryPolicy.ExecuteAsync(async () =>
            await _skillApi.GetConfigurationAsync(modelId, cancellationToken)
                .ConfigureAwait(false)).ConfigureAwait(false);
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
        if (string.IsNullOrWhiteSpace(modelId))
            throw new ArgumentException("Model ID cannot be null or empty", nameof(modelId));
        if (request == null)
            throw new ArgumentNullException(nameof(request));

        return _syncRetryPolicy.Execute(() =>
            _skillApi.PostMatchResult(modelId, _environment, request));
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
        if (string.IsNullOrWhiteSpace(modelId))
            throw new ArgumentException("Model ID cannot be null or empty", nameof(modelId));
        if (request == null)
            throw new ArgumentNullException(nameof(request));

        return _syncRetryPolicy.Execute(() =>
            _skillApi.PostPreMatch(modelId, _environment, request));
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
        if (string.IsNullOrWhiteSpace(modelId))
            throw new ArgumentException("Model ID cannot be null or empty", nameof(modelId));

        return _syncRetryPolicy.Execute(() =>
            _skillApi.GetConfiguration(modelId));
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
        private string _apiKey;
        private string _baseUrl = "https://skill.ivk.dev";
        private string _environment = "production";
        private RetryConfig _retryConfig = RetryConfig.Default;
        private HttpClient _httpClient;
        private ILogger<SkillSdk> _logger;

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
            var httpClient = _httpClient ?? new HttpClient();

            return new SkillSdk(
                _apiKey,
                _baseUrl,
                _environment,
                _retryConfig,
                httpClient,
                ownsHttpClient,
                _logger);
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
