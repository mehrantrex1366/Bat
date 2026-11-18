# Bat.Cache.Redis

`src/Cache/Bat.Cache.Redis` · namespaces `Bat.Cache.Redis`, `Bat.Cache.Hybrid` · depends on Bat.Core.

## RedisCacheProvider : IRedisCacheProvider (ISingletonInjection)
Constructors:
1. `(IOptions<RedisSettings>)` — builds `ConfigurationOptions` from settings and connects synchronously (normal DI path).
2. `(ConnectionMultiplexer, IDatabase = null)` and 3. `(ConfigurationOptions, IDatabase = null)` — `_redisSettings` is null here;
   methods that need a server fall back to the multiplexer's first endpoint.

Must be a **singleton** (one `ConnectionMultiplexer` per process). Public properties `_redisCache` (`IDatabase`) and
`_redisServer` (`ConnectionMultiplexer`) are exposed for advanced use.

API (sync + async): `Set` (string | object → JSON | arrays of pairs), `SetAndGet(<T>)`, `Get` (string | string[]),
`Get<T>`, `GetAndSet(<T>)`, `Delete`, `Rename`, `Exists`, `Length`, `Expire`, `Persist`, `Idle`, `Increment`, `Decrement`,
`Execute(command, args)`, `GetServer(host, port)`, `GetAllKey(pattern/paging)` (SCAN via `IServer.Keys`, max page 250),
`GetAllKey(RedisValue[] command)`.

Value format: objects are stored as UTF-8 JSON with Bat conventions (`SerializeToJsonUtf8Bytes` / `DeSerializeJson<T>(byte[])`),
identical bytes to the 10.0.0 string-based format, so old and new versions can share keys. `null` objects are stored as `""`.
`Get<T>` returns `default` for missing/empty values.

`CacheTools.CreateKey<T>(params)` → `"<typename lower>:<p1>:<p2>…"` (lower-cased).

## RedisSettings (`appsettings` section, usually `RedisSettings`)
`Server1/Port1 … Server3/Port3`, `Username`, `Password`, `ClientName`, `SyncTimeout` (**ms**, default 5000), `ConnectRetry`,
`ConnectTimeout` (ms), `DefaultDatabaseIndex`, `IsSentinelConnect`, `AllowAdminCommand`, `IncludeDetailInExceptions`,
`AbortOnConnectFail`, `CheckCertificateRevocation`, `SslSettings { UseSsl, SslHost, Protocol }` (optional; the old property/config key `Host` still binds and maps to `SslHost`).
At least one `ServerN` is required. The sample `appsettings.json` in the project shows the shape.

## Distributed lock
`services.AddBatDistributedLock(o => …)` registers `IDistributedLock` → `DistributedLock` (needs `IRedisCacheProvider`).
- `SetLock(key[, options])` — retries `SET key token NX EX` every `RetryDelay` ms until `AcquireTimeout` s; returns
  `DistributedLockResult` (`await using` it, or call `ReleaseAsync`, to release) or `null`.
- `TrySetLock(key, expirySeconds)` — single attempt.
- `ReleaseLock` / `RenewLock` — Lua scripts (EVALSHA) that only act if the token still matches.
- `ExecuteWithLock(key, action|func[, options])` → `bool` / `(bool Success, T Result)`; extension helpers `WithLockAsync`,
  `WithLockOrDefaultAsync`, `WithLockOrThrow`.
- `DistributedLockOptions { AcquireTimeout=10 s, LockExpiry=30 s, RetryDelay=100 ms, AutoRenew=false, RenewalInterval=10 s, LockKeyPrefix="distributed-lock" }`.
  With `AutoRenew` the lock is extended by `LockExpiry` every `RenewalInterval`.
- Keys are `"{LockKeyPrefix}:{key}"`.
- Uses `IRedisCacheProvider._redisCache` when available, otherwise falls back to `ExecuteAsync` (keeps mocks working).

## Hybrid cache
`IHybridCacheProvider` wraps `Microsoft.Extensions.Caching.Hybrid.HybridCache` (L1 = in-process `IMemoryCache`, L2 = `IDistributedCache`).

```csharp
// Redis as L2 on the SAME connection as Bat's RedisCacheProvider (RedisSettings); keys prefixed so they never collide:
services.AddBatHybridCacheWithRedis(instanceName: "crm:hc:", o => o.MaximumPayloadBytes = 1024 * 1024);
// or: services.AddStackExchangeRedisCache(...) + services.AddBatHybridCache();  or L1 only: services.AddBatHybridCache();

var user = await cache.GetOrSetAsync($"user:{id}", ct => repo.GetAsync(id, ct), TimeSpan.FromMinutes(5), ct);
await cache.GetOrSetAsync(key, factory, new HybridCacheEntryOptions { Expiration = ..., Flags = ... }, tags: ["users"], ct);
await cache.GetOrSetWithStateAsync($"user:{id}", (repo, id), static (s, ct) => s.repo.GetAsync(s.id, ct), options, ct: ct);
await cache.GetOrSetWithContextAsync(key, (ctx, ct) => { ctx.Expiration = TimeSpan.FromSeconds(30); return ...; }, options, ct: ct);
await cache.SetAsync(key, value, options, tags, ct);
await cache.DeleteByTagAsync("users", ct);
```
| Member | Notes |
|---|---|
| `AddBatHybridCache(Action<HybridCacheOptions> = null)` | `AddHybridCache` + `BatHybridCacheSerializerFactory` + provider singleton (`TryAdd`, safe twice). Implementation stays **internal**: Bat.Di never registers it on its own. |
| `AddBatHybridCacheWithRedis(instanceName = null, configure = null)` | Adds `AddStackExchangeRedisCache` whose `ConnectionMultiplexerFactory` returns `IRedisCacheProvider._redisServer` (no second connection). Needs `IRedisCacheProvider` in DI. |
| `AddBatCacheStatistics()` | `MemoryCacheOptions.TrackStatistics = true` → .NET 11 `dotnet.cache.*` metrics for the shared `IMemoryCache` (HybridCache L1). |
| `GetOrSetAsync(key, factory, expiration[, local, flags \| tags, ct])` | 10.x overloads, unchanged. L1 expiration defaults to **30 s**. The flags overload ignores tags and vice versa. |
| `GetOrSetAsync(key, factory, expiration[, ct])` | Since 11.0.0 (was CS0121 ambiguous). |
| `GetOrSetAsync(key, factory, HybridCacheEntryOptions, tags, ct)` | Since 11.0.0: flags **and** tags together; `options = null` → `HybridCacheOptions.DefaultEntryOptions` (L1 = expiration). |
| `GetOrSetWithStateAsync` / `GetOrSetWithContextAsync` / `SetAsync` | Since 11.0.0: no-closure factory; .NET 11 `HybridCacheEntryContext` (factory can change expiration / flags / size). |
| `DeleteAsync(key \| keys)`, `DeleteByTagAsync(tag)` | Removes from **this pod's** L1 and from L2. |

- **L2 payload format** (since 11.0.0): `BatHybridCacheSerializerFactory` writes Bat's JSON contract (`SerializationExtension.DefaultOptions`:
  camelCase, public fields, relaxed escaping). Without it HybridCache wrote PascalCase and **dropped public fields**. `string` and `byte[]`
  keep HybridCache's raw format. HybridCache wraps the payload in its own binary header, so its Redis values are not readable by
  `RedisCacheProvider.Get` (use separate keys / `instanceName`).
- **Multi-pod invalidation**: HybridCache (.NET 11 too) has no backplane. A delete on one pod clears L2, but other pods keep their L1 copy
  until its local expiration (30 s with the 10.x overloads). For data that must be consistent across pods use a short `localExpiration`
  or `HybridCacheEntryFlags.DisableLocalCache`.
- `AddDistributedMemoryCache()` is ignored by HybridCache as an L2 (by design).
- Metrics: hit/miss on the `Bat.Cache` meter (see Bat.Core `BatCacheMetrics`), provider `hybrid`; a caller that joined another caller's
  in-flight factory (stampede protection) counts as a hit.
- New members are default interface members: custom implementations keep compiling; `GetOrSetWithContextAsync`, `SetAsync` and
  `DeleteByTagAsync` throw `NotSupportedException` there.

## Health check and settings validation (11.0.0)
- `services.AddBatHealthChecks().AddBatRedisHealthCheck(name = "redis", failureStatus, timeout = 3 s)` — `PING` through
  `IRedisCacheProvider._redisCache`; tags `ready`, `redis` (readiness only, never liveness).
- `services.AddOptions<RedisSettings>().BindConfiguration("RedisSettings").ValidateBatRedisSettings()` — opt-in startup validation
  (`RedisSettingsValidator`): at least one server, ports 0–65535, positive timeouts, `SslHost` when `UseSsl`. `RedisSettingsValidator.GetErrors(settings)` works without DI.

## Pitfalls
- `GetAllKey` scans the whole keyspace on the server — avoid on hot paths.
- `AllowAdminCommand` is needed for some `IServer` operations.
