using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Bat.AspNetCore;

public static class HealthCheckExtensions
{
    /// <summary>Tag of the checks that run on the liveness endpoint (keep them cheap: no database / Redis calls).</summary>
    public const string LiveTag = "live";

    /// <summary>
    /// Registers health checks with a built-in <c>self</c> liveness check. Add your own readiness checks on the returned builder,
    /// e.g. <c>.AddBatRedisHealthCheck()</c> (Bat.Cache.Redis) or <c>.AddDbContextCheck&lt;MyDbContext&gt;()</c>.
    /// </summary>
    public static IHealthChecksBuilder AddBatHealthChecks(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        return services.AddHealthChecks()
            .AddCheck("self", () => HealthCheckResult.Healthy(), tags: [LiveTag]);
    }

    /// <summary>
    /// Maps Kubernetes-style probes: <paramref name="livePath"/> (only checks tagged <see cref="LiveTag"/>) and
    /// <paramref name="readyPath"/> (all checks). Both endpoints are anonymous and <b>short-circuit</b> right after routing
    /// (no authentication, CORS or custom middlewares run for probes), so they stay cheap under load.
    /// </summary>
    /// <param name="detailed">Writes a JSON body with every check's status and duration instead of the plain status text.
    /// Don't expose details publicly: check descriptions may reveal infrastructure names.</param>
    public static IEndpointRouteBuilder MapBatHealthChecks(this IEndpointRouteBuilder endpoints, string livePath = "/health/live", string readyPath = "/health/ready", bool detailed = false)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        var live = new HealthCheckOptions { Predicate = check => check.Tags.Contains(LiveTag) };
        var ready = new HealthCheckOptions();
        if (detailed)
        {
            live.ResponseWriter = WriteJsonAsync;
            ready.ResponseWriter = WriteJsonAsync;
        }

        endpoints.MapHealthChecks(livePath, live).ShortCircuit().AllowAnonymous();
        endpoints.MapHealthChecks(readyPath, ready).ShortCircuit().AllowAnonymous();
        return endpoints;
    }

    /// <summary>JSON health report writer (Bat JSON contract): <c>{ status, totalDurationMs, checks: [{ name, status, description, durationMs }] }</c>.</summary>
    public static Task WriteJsonAsync(HttpContext context, HealthReport report)
    {
        context.Response.ContentType = "application/json; charset=utf-8";
        var body = new
        {
            status = report.Status.ToString(),
            totalDurationMs = report.TotalDuration.TotalMilliseconds,
            checks = report.Entries.Select(e => new
            {
                name = e.Key,
                status = e.Value.Status.ToString(),
                description = e.Value.Description,
                durationMs = e.Value.Duration.TotalMilliseconds,
            }),
        };
        return context.Response.WriteAsync(body.SerializeToJson(), context.RequestAborted);
    }
}