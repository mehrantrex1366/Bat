namespace Bat.Cache.Hybrid;

public interface IHybridCacheProvider : ISingletonInjection
{
    ValueTask DeleteAsync(string key, CancellationToken ct);
    ValueTask DeleteAsync(ICollection<string> keys, CancellationToken ct);

    Task<TValue> GetOrSetAsync<TValue>(string key, Func<CancellationToken, ValueTask<TValue>> factory, TimeSpan expiration, TimeSpan? localExpiration = null, HybridCacheEntryFlags flags = HybridCacheEntryFlags.None, CancellationToken ct = default);
    Task<TValue> GetOrSetAsync<TValue>(string key, Func<CancellationToken, ValueTask<TValue>> factory, TimeSpan expiration, TimeSpan? localExpiration = null, ICollection<string> tags = null, CancellationToken ct = default);
}