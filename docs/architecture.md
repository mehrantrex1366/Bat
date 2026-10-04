# Architecture

## Package dependency graph

```
                          Bat.Core  (no Bat dependencies)
                             ▲
   ┌──────────┬──────────┬───┴──────┬───────────┬──────────┬──────────┬──────────┬──────────┐
Bat.Tools  Bat.Cache  Bat.Cache.Redis  Bat.Http  Bat.AspNetCore  Bat.Di  Bat.Dapper  Bat.Queue  Bat.EntityFrameworkCore
                                                                                                  ▲
                                                                                  Bat.EntityFrameworkCore.Tools
                                                                                                  ▲
                                                                                              Bat.Test
tests/Bat.Regression.Tests → Bat.Http, Bat.AspNetCore, Bat.Cache, Bat.Dapper, Bat.EntityFrameworkCore.Tools
Bat.SqlClrAssembly (.NET Framework, standalone; contains its own copies of PersianDateTime, MaskMode, MaskOption)
```

Every package depends on **Bat.Core**. A change in Bat.Core ships to every consumer, which is why all packages share
one version number (see [build-test-release.md](build-test-release.md)).

## Third-party dependencies (as of 10.0.2)

| Package | Main dependencies |
|---|---|
| Bat.Core | none (BCL only) |
| Bat.Tools | EPPlus 8.x, EPPlus.DataExtractor, System.Security.Cryptography.Xml (pinned, vulnerability fix) |
| Bat.Cache | System.Runtime.Caching |
| Bat.Cache.Redis | StackExchange.Redis 3.x, Microsoft.Extensions.Caching.StackExchangeRedis, Microsoft.Extensions.Caching.Hybrid |
| Bat.Http | Microsoft.AspNetCore.Http.Abstractions 2.3 (for `HttpContext`), Polly / Microsoft.Extensions.Http.Polly (referenced, not used by code) |
| Bat.AspNetCore | `Microsoft.NET.Sdk.Web`, JwtBearer, Swashbuckle 10, Microsoft.OpenApi (pinned) |
| Bat.Di | Autofac.Extensions.DependencyInjection, DryIoc.Microsoft.DependencyInjection |
| Bat.EntityFrameworkCore | EF Core 10 + SqlServer provider |
| Bat.EntityFrameworkCore.Tools | EFCore.BulkExtensions.SqlServer |
| Bat.Dapper | Dapper, Microsoft.Data.SqlClient |
| Bat.Queue | RabbitMQ.Client 7, Experimental.System.Messaging (MSMQ, Windows only) |
| Bat.Test | Moq, NSubstitute, AutoFixture.AutoMoq, AutoBogus, MockQueryable |

## Cross-cutting design

### Dependency-injection marker interfaces (Bat.Core → Bat.Di)
Classes opt in to auto-registration by implementing a marker interface from Bat.Core:
`ITransientInjection`, `IScopedInjection`, `ISingletonInjection`.
Bat.Di scans **all loaded, non-dynamic assemblies** for public, non-abstract classes implementing the marker and
registers each class under **the first implemented interface whose name contains the class name**
(`RedisCacheProvider` → `IRedisCacheProvider`); if none matches, the class is registered as itself.
Some Bat types are pre-marked: `IMemoryCacheProvider` and `IRedisCacheProvider` (singleton),
`IHybridCacheProvider` (singleton, but its implementation is `internal`, see known issues),
`IEFGenericRepo<T>` / `IEFBulkGenericRepo<T>` (transient).

### Entity marker interfaces (Bat.Core → Bat.EntityFrameworkCore)
`IBaseEntity` marks entities usable by the generic repositories. `IBaseProperties` and the `IInsert*/IModify*/
ISoftDeleteProperty/IIsActiveProperty` interfaces are filled automatically by `BatDbContext.SaveChanges*`
(see [reference/Bat.EntityFrameworkCore.md](reference/Bat.EntityFrameworkCore.md)).

### JSON
All JSON in the framework goes through `Bat.Core.SerializationExtension` (System.Text.Json) with one shared,
read-only options instance. Redis values, HTTP bodies, RabbitMQ messages, session values, menu JSON, audit logs and
the `ObjectToJsonConverter` all depend on its exact conventions. See [reference/Bat.Core.md](reference/Bat.Core.md) (section "Serialization").

### Response pattern
`Response` / `Response<T>` (`IsSuccess`, `Message`, `ResultCode` default 200, `Result`, `ExecutionTime`) is the
standard service/API result. Middlewares in Bat.AspNetCore return anonymous objects with
`isSuccessful` / `resultCode` / `message` (note: `isSuccessful`, not `isSuccess`).

### Persian / Iran specifics
- `PersianDateTime` (Solar Hijri calendar) with `PersianDateTime.Now` = Iran time (UTC+03:30, no DST).
- Persian `ی/ک` normalization and Persian/Arabic digit → ASCII conversion are applied to **every string property of
  every tracked entity** on `BatDbContext.SaveChanges*`.
- Validators for Iranian mobile numbers, national code, IBAN (sheba), car plates, bank cards.

### Hosting assumptions
- Linux containers (Kubernetes) and Windows servers. Use `Path.Combine`; local time zone may be UTC.
- Process-wide shared resources (JSON options, HTTP clients, regexes, reflection caches, derived crypto keys) are
  static and thread-safe by design. Don't add per-call allocations of these.
