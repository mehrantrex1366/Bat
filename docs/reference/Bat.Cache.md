# Bat.Cache

`src/Cache/Bat.Cache` · namespace `Bat.Cache` · depends on Bat.Core and System.Runtime.Caching.

## MemoryCacheProvider : IMemoryCacheProvider (ISingletonInjection)
Wraps one `System.Runtime.Caching.MemoryCache` (name `Bat.MemoryCache` or the ctor argument). Register as a singleton.

| Member | Behavior |
|---|---|
| `Set(key, value, expireTime[, slidingTime])` | **Inserts or overwrites** and returns `true` (10.0.0 used `Add`, which silently kept the old value). Absolute + sliding together throws (System.Runtime.Caching rule). |
| `Add(key, value, CacheItemPolicy)` | Add-if-absent; `false` if the key exists. |
| `GetSet(key, value, expire)` / `AddOrGetExisting` | Returns the existing value, or `null` after adding. |
| `Get`, `GetCacheItem`, `GetAll(keys)`, `Delete` | — |
| `GetCount`, `GetSize` (`GetLastSize`), `GetMemoryLimit`, `TrimMemory(%)` | Diagnostics. |

For new code in ASP.NET Core prefer `Microsoft.Extensions.Caching.Memory` / HybridCache; this provider stays for compatibility.
