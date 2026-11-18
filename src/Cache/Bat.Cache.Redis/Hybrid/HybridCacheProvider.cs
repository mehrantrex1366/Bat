namespace Bat.Cache.Hybrid;

internal class HybridCacheProvider : IHybridCacheProvider
{
    private const string MetricsProvider = "hybrid";

    public readonly HybridCache _hybridCache;

    public HybridCacheProvider(HybridCache hybridCache)
    {
        _hybridCache = hybridCache;
    }


    public async ValueTask DeleteAsync(string key, CancellationToken ct)
    {
        await _hybridCache.RemoveAsync(key, ct);
    }

    public async ValueTask DeleteAsync(ICollection<string> keys, CancellationToken ct)
    {
        await _hybridCache.RemoveAsync(keys, ct);
    }

    public ValueTask DeleteByTagAsync(string tag, CancellationToken ct = default)
        => _hybridCache.RemoveByTagAsync(tag, ct);

    public ValueTask SetAsync<TValue>(string key, TValue value, HybridCacheEntryOptions options = null, IEnumerable<string> tags = null, CancellationToken ct = default)
        => _hybridCache.SetAsync(key, value, options, tags, ct);


    public Task<TValue> GetOrSetAsync<TValue>(string key, Func<CancellationToken, ValueTask<TValue>> factory, TimeSpan expiration, TimeSpan? localExpiration = null, HybridCacheEntryFlags flags = HybridCacheEntryFlags.None, CancellationToken ct = default)
    {
        var options = new HybridCacheEntryOptions
        {
            Flags = flags,
            Expiration = expiration,
            LocalCacheExpiration = localExpiration ?? TimeSpan.FromSeconds(30)
        };

        return GetOrSetAsync(key, factory, options, null, ct);
    }

    public Task<TValue> GetOrSetAsync<TValue>(string key, Func<CancellationToken, ValueTask<TValue>> factory, TimeSpan expiration, TimeSpan? localExpiration = null, ICollection<string> tags = null, CancellationToken ct = default)
    {
        var options = new HybridCacheEntryOptions
        {
            Expiration = expiration,
            LocalCacheExpiration = localExpiration ?? TimeSpan.FromSeconds(30)
        };

        return GetOrSetAsync(key, factory, options, tags, ct);
    }

    public Task<TValue> GetOrSetAsync<TValue>(string key, Func<CancellationToken, ValueTask<TValue>> factory, HybridCacheEntryOptions options, IEnumerable<string> tags = null, CancellationToken ct = default)
        => GetOrSetWithStateAsync(key, factory, static (f, c) => f(c), options, tags, ct);

    public async Task<TValue> GetOrSetWithStateAsync<TState, TValue>(string key, TState state, Func<TState, CancellationToken, ValueTask<TValue>> factory, HybridCacheEntryOptions options = null, IEnumerable<string> tags = null, CancellationToken ct = default)
    {
        // Metrics cost nothing unless a listener (OpenTelemetry, dotnet-counters) subscribed to the "Bat.Cache" meter.
        if (!BatCacheMetrics.IsEnabled)
            return await _hybridCache.GetOrCreateAsync(key, state, factory, options, tags, ct);

        var tracker = new MissTracker<TState, TValue>(state, factory);
        var value = await _hybridCache.GetOrCreateAsync(key, tracker, static (t, c) => t.Invoke(c), options, tags, ct);
        BatCacheMetrics.RecordRequest(MetricsProvider, hit: !tracker.Missed);
        return value;
    }

    public async Task<TValue> GetOrSetWithContextAsync<TValue>(string key, Func<HybridCacheEntryContext, CancellationToken, ValueTask<TValue>> factory, HybridCacheEntryOptions options = null, IEnumerable<string> tags = null, CancellationToken ct = default)
    {
        if (!BatCacheMetrics.IsEnabled)
            return await _hybridCache.GetOrCreateAsync(key, factory, options, tags, ct);

        var missed = false;
        var value = await _hybridCache.GetOrCreateAsync(key, (context, c) => { missed = true; return factory(context, c); }, options, tags, ct);
        BatCacheMetrics.RecordRequest(MetricsProvider, hit: !missed);
        return value;
    }

    private sealed class MissTracker<TState, TValue>(TState state, Func<TState, CancellationToken, ValueTask<TValue>> factory)
    {
        public bool Missed { get; private set; }

        public ValueTask<TValue> Invoke(CancellationToken ct)
        {
            Missed = true;
            return factory(state, ct);
        }
    }
}
