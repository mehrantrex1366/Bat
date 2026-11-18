namespace Bat.Cache.Hybrid;

public interface IHybridCacheProvider : ISingletonInjection
{
    ValueTask DeleteAsync(string key, CancellationToken ct);
    ValueTask DeleteAsync(ICollection<string> keys, CancellationToken ct);

    Task<TValue> GetOrSetAsync<TValue>(string key, Func<CancellationToken, ValueTask<TValue>> factory, TimeSpan expiration, TimeSpan? localExpiration = null, HybridCacheEntryFlags flags = HybridCacheEntryFlags.None, CancellationToken ct = default);
    Task<TValue> GetOrSetAsync<TValue>(string key, Func<CancellationToken, ValueTask<TValue>> factory, TimeSpan expiration, TimeSpan? localExpiration = null, ICollection<string> tags = null, CancellationToken ct = default);

    // Members below were added in 11.0.0 as default interface members, so existing implementations and mocks keep compiling.
    // New overloads use a distinct parameter type (HybridCacheEntryOptions) or a distinct name, so no existing call becomes ambiguous.

    // GetOrSetAsync(key, factory, expiration[, ct]) was ambiguous (CS0121) between the two overloads above,
    // because both have only optional parameters after `expiration`.
    Task<TValue> GetOrSetAsync<TValue>(string key, Func<CancellationToken, ValueTask<TValue>> factory, TimeSpan expiration)
        => GetOrSetAsync(key, factory, expiration, null, HybridCacheEntryFlags.None, CancellationToken.None);

    Task<TValue> GetOrSetAsync<TValue>(string key, Func<CancellationToken, ValueTask<TValue>> factory, TimeSpan expiration, CancellationToken ct)
        => GetOrSetAsync(key, factory, expiration, null, HybridCacheEntryFlags.None, ct);

    /// <summary>
    /// Full control: expiration, local expiration, flags <b>and</b> tags together (the two overloads above support either flags or tags).
    /// <paramref name="options"/> = <c>null</c> uses <c>HybridCacheOptions.DefaultEntryOptions</c>.
    /// </summary>
    Task<TValue> GetOrSetAsync<TValue>(string key, Func<CancellationToken, ValueTask<TValue>> factory, HybridCacheEntryOptions options, IEnumerable<string> tags = null, CancellationToken ct = default)
    {
        // Fallback for custom implementations: maps to the older overloads (flags and tags cannot be combined there).
        var expiration = options?.Expiration ?? TimeSpan.FromMinutes(5);
        return tags is null
            ? GetOrSetAsync(key, factory, expiration, options?.LocalCacheExpiration, options?.Flags ?? HybridCacheEntryFlags.None, ct)
            : GetOrSetAsync(key, factory, expiration, options?.LocalCacheExpiration, tags as ICollection<string> ?? tags.ToList(), ct);
    }

    /// <summary>
    /// Like <c>GetOrSetAsync</c>, but passes <paramref name="state"/> to a <c>static</c> factory, so hot paths don't allocate a closure:
    /// <c>GetOrSetWithStateAsync($"user:{id}", (repo, id), static (s, ct) => s.repo.GetAsync(s.id, ct), options)</c>.
    /// </summary>
    Task<TValue> GetOrSetWithStateAsync<TState, TValue>(string key, TState state, Func<TState, CancellationToken, ValueTask<TValue>> factory, HybridCacheEntryOptions options = null, IEnumerable<string> tags = null, CancellationToken ct = default)
        => GetOrSetAsync(key, c => factory(state, c), options, tags, ct);

    /// <summary>
    /// .NET 11 HybridCache factory context: the factory can change the entry's <c>Expiration</c>, <c>LocalCacheExpiration</c>,
    /// <c>Flags</c> or <c>LocalSize</c> after it sees the value (e.g. cache "not found" results for a shorter time).
    /// </summary>
    Task<TValue> GetOrSetWithContextAsync<TValue>(string key, Func<HybridCacheEntryContext, CancellationToken, ValueTask<TValue>> factory, HybridCacheEntryOptions options = null, IEnumerable<string> tags = null, CancellationToken ct = default)
        => throw new NotSupportedException($"{GetType().Name} does not support the HybridCache factory context.");

    /// <summary>Writes a value to the cache (L1 and L2) without reading it first.</summary>
    ValueTask SetAsync<TValue>(string key, TValue value, HybridCacheEntryOptions options = null, IEnumerable<string> tags = null, CancellationToken ct = default)
        => throw new NotSupportedException($"{GetType().Name} does not support SetAsync.");

    /// <summary>Removes every entry that was stored with one of the given tags (see <see cref="HybridCache.RemoveByTagAsync(string, CancellationToken)"/>).</summary>
    ValueTask DeleteByTagAsync(string tag, CancellationToken ct = default)
        => throw new NotSupportedException($"{GetType().Name} does not support tag invalidation.");
}
