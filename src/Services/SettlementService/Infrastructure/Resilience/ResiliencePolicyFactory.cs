using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;
using Polly.Timeout;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using System.Data;

namespace AresNexus.Services.Settlement.Infrastructure.Resilience;

/// <summary>
/// Provides resilience policies for database operations.
/// </summary>
public interface IResiliencePolicyFactory
{
    /// <summary>
    /// Gets the database resilience policy.
    /// </summary>
    /// <returns>An async policy.</returns>
    IAsyncPolicy GetDatabasePolicy();
}

/// <summary>
/// Default implementation of the resilience policy factory.
/// </summary>
public sealed class ResiliencePolicyFactory : IResiliencePolicyFactory
{
    private readonly ILogger<ResiliencePolicyFactory> _logger;
    private readonly IAsyncPolicy _databasePolicy;

    /// <summary>
    /// Initializes a new instance of the <see cref="ResiliencePolicyFactory"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="configuration">The configuration.</param>
    public ResiliencePolicyFactory(ILogger<ResiliencePolicyFactory> logger, IConfiguration configuration)
    {
        _logger = logger;
        
        var retryCount = configuration.GetValue("Resilience:Database:RetryCount", 3);
        var breakDuration = configuration.GetValue("Resilience:Database:CircuitBreakDurationSeconds", 30);
        var timeoutSeconds = configuration.GetValue("Resilience:Database:TimeoutSeconds", 10);

        var retryPolicy = Policy
            .Handle<DataException>()
            .Or<TimeoutException>()
            .Or<Marten.Exceptions.MartenException>()
            .WaitAndRetryAsync(
                retryCount,
                retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                (exception, timeSpan, retry, context) =>
                {
                    _logger.LogWarning(exception, "Database operation failed. Retry {Retry} of {RetryCount} after {Delay}ms.", retry, retryCount, timeSpan.TotalMilliseconds);
                });

        var circuitBreakerPolicy = Policy
            .Handle<DataException>()
            .Or<TimeoutException>()
            .Or<Marten.Exceptions.MartenException>()
            .CircuitBreakerAsync(
                exceptionsAllowedBeforeBreaking: 5,
                durationOfBreak: TimeSpan.FromSeconds(breakDuration),
                onBreak: (exception, duration) =>
                {
                    _logger.LogCritical(exception, "Circuit breaker OPEN for {Duration}s due to consecutive failures.", duration.TotalSeconds);
                },
                onReset: () => _logger.LogInformation("Circuit breaker RESET. Database operations resumed."));

        var timeoutPolicy = Policy.TimeoutAsync(TimeSpan.FromSeconds(timeoutSeconds), TimeoutStrategy.Optimistic);

        _databasePolicy = Policy.WrapAsync(retryPolicy, circuitBreakerPolicy, timeoutPolicy);
    }

    /// <inheritdoc />
    public IAsyncPolicy GetDatabasePolicy() => _databasePolicy;
}
