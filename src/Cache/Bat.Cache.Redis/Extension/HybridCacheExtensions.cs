using Bat.Cache.Redis;
using Microsoft.Extensions.Caching.StackExchangeRedis;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Bat.Cache.Hybrid;

public static class HybridCacheExtensions
{
    /// <summary>
    /// Registers <see cref="HybridCache"/> (<c>AddHybridCache</c>) and Bat's <see cref="IHybridCacheProvider"/> wrapper (singleton).
    /// Values are stored in L2 with Bat's JSON contract (<see cref="BatHybridCacheSerializerFactory"/>).
    /// If an <c>IDistributedCache</c> is registered (e.g. <c>AddStackExchangeRedisCache</c> or <see cref="AddBatHybridCacheWithRedis"/>),
    /// HybridCache uses it as its L2 cache (the in-memory <c>AddDistributedMemoryCache</c> is ignored by HybridCache on purpose).
    /// </summary>
    /// <remarks>
    /// <c>HybridCacheProvider</c> stays internal so Bat.Di auto-registration never picks it up in services that don't use HybridCache;
    /// this method is the explicit way to opt in. Calling it more than once is safe.
    /// </remarks>
    public static IServiceCollection AddBatHybridCache(this IServiceCollection services, Action<HybridCacheOptions> configureOptions = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        if (configureOptions is null)
            services.AddHybridCache();
        else
            services.AddHybridCache(configureOptions);

        services.TryAddEnumerable(ServiceDescriptor.Singleton<IHybridCacheSerializerFactory, BatHybridCacheSerializerFactory>());
        services.TryAddSingleton<IHybridCacheProvider, HybridCacheProvider>();

        return services;
    }

    /// <summary>
    /// <see cref="AddBatHybridCache"/> with Redis as L2, <b>reusing the connection of Bat's <see cref="IRedisCacheProvider"/></b>
    /// (configured from <see cref="RedisSettings"/>) instead of opening a second <c>ConnectionMultiplexer</c>.
    /// </summary>
    /// <param name="instanceName">
    /// Prefix added by HybridCache to its Redis keys (e.g. <c>"crm:hc:"</c>), so they never collide with keys written by
    /// <see cref="RedisCacheProvider"/> or by other services that share the same Redis database.
    /// </param>
    /// <remarks>
    /// Requires <see cref="IRedisCacheProvider"/> in DI (Bat.Di registers it as a singleton). Its connection is resolved on first use.
    /// </remarks>
    public static IServiceCollection AddBatHybridCacheWithRedis(this IServiceCollection services, string instanceName = null, Action<HybridCacheOptions> configureOptions = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddStackExchangeRedisCache(options => options.InstanceName = instanceName);
        services.AddOptions<RedisCacheOptions>()
            .Configure<IServiceProvider>((options, provider) =>
                options.ConnectionMultiplexerFactory = () =>
                    Task.FromResult<IConnectionMultiplexer>(provider.GetRequiredService<IRedisCacheProvider>()._redisServer));

        return services.AddBatHybridCache(configureOptions);
    }

    /// <summary>
    /// Turns on .NET 11 <c>MemoryCache</c> statistics (<c>MemoryCacheOptions.TrackStatistics</c>) for the shared <c>IMemoryCache</c>,
    /// which HybridCache uses as its L1. .NET publishes them as the <c>dotnet.cache.requests</c> / <c>evictions</c> / <c>entries</c> /
    /// <c>estimated_size</c> instruments; Bat's own instruments are on the <see cref="BatCacheMetrics.MeterName"/> meter.
    /// </summary>
    public static IServiceCollection AddBatCacheStatistics(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddMemoryCache(options => options.TrackStatistics = true);
        return services;
    }
}