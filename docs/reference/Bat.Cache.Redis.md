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
`IHybridCacheProvider` (`GetOrSetAsync` with expiration/local expiration/flags/tags, `DeleteAsync`) wraps
`Microsoft.Extensions.Caching.Hybrid.HybridCache`. The implementation `HybridCacheProvider` is **internal**, so neither
Bat.Di nor consumers can register it today (see known issues).

## Pitfalls
- `GetAllKey` scans the whole keyspace on the server — avoid on hot paths.
- `AllowAdminCommand` is needed for some `IServer` operations.
