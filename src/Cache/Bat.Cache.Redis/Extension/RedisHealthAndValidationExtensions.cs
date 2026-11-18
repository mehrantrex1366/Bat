using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Bat.Cache.Redis;

/// <summary>Readiness check: <c>PING</c> through the connection of Bat's <see cref="IRedisCacheProvider"/>.</summary>
public sealed class RedisHealthCheck(IRedisCacheProvider redisCacheProvider) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var latency = await redisCacheProvider._redisCache.PingAsync().WaitAsync(cancellationToken);
            return HealthCheckResult.Healthy($"PING {latency.TotalMilliseconds:0.#} ms");
        }
        catch (Exception ex) when (ex is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
        {
            return new HealthCheckResult(context.Registration.FailureStatus, "Redis is not reachable.", ex);
        }
    }
}

/// <summary>Startup validation of <see cref="RedisSettings"/> (opt-in, see <see cref="RedisExtensions.ValidateBatRedisSettings"/>).</summary>
public sealed class RedisSettingsValidator : IValidateOptions<RedisSettings>
{
    public ValidateOptionsResult Validate(string name, RedisSettings options)
    {
        var errors = GetErrors(options);
        return errors.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(errors);
    }

    public static IReadOnlyList<string> GetErrors(RedisSettings settings)
    {
        var errors = new List<string>();
        if (settings is null)
        {
            errors.Add("RedisSettings is not configured.");
            return errors;
        }

        if (settings.Server1.IsNullOrWhiteSpace() && settings.Server2.IsNullOrWhiteSpace() && settings.Server3.IsNullOrWhiteSpace())
            errors.Add("RedisSettings: at least one of Server1, Server2, Server3 is required.");

        CheckPort(settings.Server1, settings.Port1, nameof(RedisSettings.Port1), errors);
        CheckPort(settings.Server2, settings.Port2, nameof(RedisSettings.Port2), errors);
        CheckPort(settings.Server3, settings.Port3, nameof(RedisSettings.Port3), errors);

        if (settings.SyncTimeout <= 0) errors.Add("RedisSettings.SyncTimeout must be greater than 0.");
        if (settings.ConnectTimeout <= 0) errors.Add("RedisSettings.ConnectTimeout must be greater than 0.");
        if (settings.ConnectRetry < 0) errors.Add("RedisSettings.ConnectRetry cannot be negative.");
        if (settings.DefaultDatabaseIndex < 0) errors.Add("RedisSettings.DefaultDatabaseIndex cannot be negative.");
        if (settings.SslSettings?.UseSsl == true && settings.SslSettings.SslHost.IsNullOrWhiteSpace())
            errors.Add("RedisSettings.SslSettings.SslHost is required when UseSsl is true.");

        return errors;
    }

    private static void CheckPort(string server, int port, string name, List<string> errors)
    {
        if (!server.IsNullOrWhiteSpace() && port is < 0 or > 65535)
            errors.Add($"RedisSettings.{name} must be between 0 and 65535 (0 = default 6379).");
    }
}

public static class RedisExtensions
{
    /// <summary>Adds <see cref="RedisHealthCheck"/> (tags <c>ready</c>, <c>redis</c>) to the health checks.</summary>
    public static IHealthChecksBuilder AddBatRedisHealthCheck(this IHealthChecksBuilder builder, string name = "redis", HealthStatus failureStatus = HealthStatus.Unhealthy, TimeSpan? timeout = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        return builder.AddCheck<RedisHealthCheck>(name, failureStatus, ["ready", "redis"], timeout ?? TimeSpan.FromSeconds(3));
    }

    /// <summary>
    /// Validates the bound <see cref="RedisSettings"/> when the host starts. Opt-in.
    /// <code>services.AddOptions&lt;RedisSettings&gt;().BindConfiguration("RedisSettings").ValidateBatRedisSettings();</code>
    /// </summary>
    public static OptionsBuilder<RedisSettings> ValidateBatRedisSettings(this OptionsBuilder<RedisSettings> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        return builder.Validate<RedisSettingsValidator>().ValidateOnStart();
    }
}