using System.Diagnostics.Metrics;

namespace Bat.Core;

/// <summary>
/// Cache hit/miss metrics of Bat's cache providers (<c>MemoryCacheProvider</c>, <c>IHybridCacheProvider</c>).
/// Subscribe with OpenTelemetry (<c>.WithMetrics(m =&gt; m.AddMeter(BatCacheMetrics.MeterName))</c>) or
/// <c>dotnet-counters monitor --counters Bat.Cache</c>. Recording is skipped when nobody listens.
/// </summary>
public static class BatCacheMetrics
{
    public const string MeterName = "Bat.Cache";
    public const string RequestsInstrumentName = "bat.cache.requests";
    public const string ProviderTagName = "bat.cache.provider";
    public const string ResultTagName = "bat.cache.request.result";

    private static readonly Meter _meter = new(MeterName, typeof(BatCacheMetrics).Assembly.GetName().Version?.ToString());

    private static readonly Counter<long> _requests = _meter.CreateCounter<long>(
        RequestsInstrumentName, unit: "{request}", description: "Cache lookups by provider and result (hit or miss).");

    /// <summary><c>true</c> when at least one listener is subscribed to <see cref="RequestsInstrumentName"/>.</summary>
    public static bool IsEnabled => _requests.Enabled;

    /// <summary>Records one cache lookup. Cheap no-op when no listener is subscribed.</summary>
    public static void RecordRequest(string provider, bool hit)
    {
        if (!_requests.Enabled)
            return;

        _requests.Add(1,
            new KeyValuePair<string, object>(ProviderTagName, provider),
            new KeyValuePair<string, object>(ResultTagName, hit ? "hit" : "miss"));
    }
}