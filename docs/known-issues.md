# Known issues and technical debt

Items found during the 10.0.1 review that were **not** changed, because fixing them would break consumers, stored data
or public API, or needs a decision from the owner. Fix them only with an explicit decision and a `CHANGELOG.md` entry.

## Security debt
| Item | Where | Why not fixed |
|---|---|---|
| Hard-coded default pass phrase, salt and IV; `PasswordDeriveBytes` with 1 iteration and MD5; static IV (same plaintext → same ciphertext) | `Encryption`, `AesEncryption`, EF encryptor value converters | Existing encrypted data in databases depends on it. A fix needs a new versioned format (e.g. AES-GCM, random IV, PBKDF2/HKDF, key from configuration) + data migration. |
| Hard-coded fallback JWT signing/encryption keys when `JwtSettings` keys are empty | `JwtConfiguration`, `JwtService` | Changing would invalidate tokens of services that rely on the fallback. Services should always configure keys. |
| `HashGenerator.Hash(key)` uses a hard-coded salt and a single SHA-256 | `HashGenerator` | Stored hashes depend on it. Use a password hasher (PBKDF2/Argon2) for new password storage. |
| Certificate validation bypass is the **default** in `PostFormFileAsync(url, …)` and `PutFormAsync(url, …)` (`byPassServerCertificate = true`) | `HttpRequestTools` | Changing the default could break calls to servers with self-signed certificates. Callers should pass `false`. |
| `RequireHttpsMetadata = false` in JwtBearer setup | `JwtConfiguration` | Symmetric keys only; kept. |

## Behavior / design issues
- `BasePropertiesInitializer` uses the server's local clock (`DateTime.Now`) for all `*Mi` and `*Sh` values (since 10.0.1 also for
  `ModifyDateSh`, which used Iran time before). Containers running in UTC therefore store UTC-based dates; set `TZ=Asia/Tehran`
  in the deployment if Iran time is required.
- `AuthorizationFilter` is synchronous (`IAuthorizationFilter`) and calls `IUserActionProvider.GetUserActions` (sync) on every
  request. `OnAuthorizationAsync` exists but MVC never calls it. Making it `IAsyncAuthorizationFilter` would switch every service
  to `GetUserActionsAsync` — coordinate with consumers.
- ~~`HybridCacheProvider` is `internal`, so `IHybridCacheProvider` cannot be resolved.~~ Fixed in 11.0.0: `services.AddBatHybridCache()`
  registers it (the class stays internal so Bat.Di never auto-registers it).
- `IHybridCacheProvider.GetOrSetAsync(..., flags, ...)` ignores tags and `GetOrSetAsync(..., tags, ...)` ignores flags (10.x overloads, kept
  as they are). Since 11.0.0 use the `GetOrSetAsync(key, factory, HybridCacheEntryOptions, tags, ct)` overload for both.
- HybridCache has no backplane (also in .NET 11): a delete on one pod leaves other pods' L1 copies until their local expiration
  (30 s by default in Bat's 10.x overloads). Use a short `localExpiration` or `DisableLocalCache` for data that must be consistent.
- `IntEncryptorConverter` / `GuidEncryptorConverter` format the value with the current culture (`x.ToString()`); with `fa-IR` the
  minus sign of negative numbers may differ from other servers, so the same negative value can be stored as a different ciphertext.
  Not changed: stored data depends on it (rule 2).
- `DbContextFactory` hands out process-wide `DbContext` singletons (not thread-safe, unbounded change tracker).
- `EFGenericRepo<T>` does not implement `IEFGenericRepo<T>`; factories cast the resolved service to the concrete class.
- `PartialUpdate` sets the entry to `Detached` before marking properties modified (re-attaches implicitly); unusual but relied on.
- `SaveAuditLog` issues one synchronous `GetDatabaseValues()` query per modified entity.
- `DapperGenericRepo.GetPaging` pages in memory; `DapperGenericRepo` keeps one `SqlConnection` per instance (not thread-safe).
- `RabbitConsumer` uses `autoAck: true` (no redelivery on handler failure) and disposes connections passed in by the caller.
- `Bat.Di` scans all loaded assemblies and ignores the `assembly` parameter.
- `FileLoger` is synchronous file I/O under a global lock; prefer `ILogger` in services.
- `Response.ExecutionTime` returns `DateTime.Now` at read time, not the execution time.
- `ClaimsExtensions.GetUserId_Str()` returns `int` despite its name.
- `ValidatorExtensions.IsBankSheba` only checks the length; `IsIban` does the real check.
- `NumberExtensions.ToCurrency/ToNumeric` and `PersianDateTime.ToString()` depend on the current culture by design.
- `FileConvertor.ToStandardSize(size, in, out)` and `ToNewSize` are unimplemented stubs.
- `Bat.Http` references Polly packages but performs no retries.
- `Bat.SqlClrAssembly` has its own copy of `PersianDateTime` (old DST logic, old month-name bug).

## Dependencies
- `EFCore.BulkExtensions.SqlServer` has no EF Core 11 build yet (latest 10.0.1). Bat 11 keeps 10.0.1 on top of EF Core 11: it compiles
  and resolves (NuGet lower bounds), but the library uses EF Core internals and was **not** verified against SQL Server with EF Core 11.
  Test `EFBulkGenericRepo` in a service before release and move to the EF 11 build of BulkExtensions as soon as it is published.
- `AutoFixture.AutoMoq` 5 is still a release candidate; Bat.Test stays on 4.18.1. Prereleases (StackExchange.Redis 4 alpha,
  Microsoft.Data.SqlClient 8 preview, EPPlus 9 preview, RabbitMQ.Client 7.3 beta) are not used.

## Packaging
- `Bat.Cache.Redis` packs its sample `appsettings.json` as content (`content/` + `contentFiles/`), so it is added to consuming
  projects. Kept because removing it changes what existing consumers get; set `Pack="false"` on it once agreed.
- `Bat.AspNetCore` uses `Microsoft.NET.Sdk.Web` and is an `Exe` with an empty public `Bat.AspNetCore.Mvc.Program.Main`
  (public type, so not removed). The package therefore also contains `Bat.AspNetCore.runtimeconfig.json` (harmless).
  Its `appsettings.json` is excluded from the package (`Pack="false"`).
- `Bat.Di` depends on the prerelease `DryIoc.Microsoft.DependencyInjection 8.0.0-preview-04` → pack warning `NU5104`.

## Not reviewed in depth
`Bat.Test` builders, Razor tag helpers, `BatSwaggerFileFilter` (compiled and read, no runtime test).
The other Swagger filters have runtime tests since 11.0.0 (`SwaggerTests.cs`).
