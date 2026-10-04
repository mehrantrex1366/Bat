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
- `HybridCacheProvider` is `internal`, so `IHybridCacheProvider` cannot be resolved. Making it public would make Bat.Di
  auto-register it and fail DI validation in services that don't call `AddHybridCache()`. Needs an explicit `AddBatHybridCache()` instead.
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

## Not reviewed in depth
`Bat.Test` builders, Razor tag helpers, Swagger filters (compiled and read, no runtime test).
