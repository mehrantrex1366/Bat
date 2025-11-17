namespace Bat.Cache.Hybrid;

internal class HybridCacheProvider : IHybridCacheProvider
{
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


    public async Task<TValue> GetOrSetAsync<TValue>(string key, Func<CancellationToken, ValueTask<TValue>> factory, TimeSpan expiration, TimeSpan? localExpiration = null, HybridCacheEntryFlags flags = HybridCacheEntryFlags.None, CancellationToken ct = default)
    {
        var options = new HybridCacheEntryOptions
        {
            Flags = flags,
            Expiration = expiration,
            LocalCacheExpiration = localExpiration ?? TimeSpan.FromSeconds(30)
        };

        return await _hybridCache.GetOrCreateAsync<TValue>(
            key: key,
            factory: factory,
            options: options,
            cancellationToken: ct);
    }

    public async Task<TValue> GetOrSetAsync<TValue>(string key, Func<CancellationToken, ValueTask<TValue>> factory, TimeSpan expiration, TimeSpan? localExpiration = null, ICollection<string> tags = null, CancellationToken ct = default)
    {
        var options = new HybridCacheEntryOptions
        {
            Expiration = expiration,
            LocalCacheExpiration = localExpiration ?? TimeSpan.FromSeconds(30)
        };

        return await _hybridCache.GetOrCreateAsync<TValue>(
            key: key,
            factory: factory,
            options: options,
            tags: tags,
            cancellationToken: ct);
    }
}