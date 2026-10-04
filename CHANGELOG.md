# Changelog

All packages share one version. Newest first.

## 10.0.1 — performance and correctness review (2026-10)

No public type or member was removed or renamed; all changes are source compatible.
Encryption output and the JSON format are unchanged (pinned by `tests/Bat.Regression.Tests`).
**Update all Bat packages together** in each service.

### Performance
- **JSON (Bat.Core):** `SerializeToJson`, `DeSerializeJson*`, `DeSerializeJsonToDynamic` no longer create a new
  `JsonSerializerOptions` (with new converter instances) per call. A shared read-only instance (and one per `MaxDepth`)
  is used, so System.Text.Json's metadata cache is reused instead of being rebuilt — ~40× faster and ~30× fewer
  allocations per round-trip in a micro-benchmark; removes the steady creation of DynamicMethods/JIT code seen in production
  dumps. This automatically benefits Bat.Cache.Redis, Bat.Http, Bat.AspNetCore (middlewares, session), Bat.Queue and menus.
  New: `SerializationExtension.DefaultOptions`, `GetSharedOption(depth)`, `SerializeToJsonUtf8Bytes()`, `DeSerializeJson<T>(byte[])`.
  `GetDefaultOption()` still returns a new mutable instance.
- **HTTP (Bat.Http):** `HttpRequestTools` no longer creates an `HttpClient`/`HttpClientHandler` per call (socket exhaustion,
  no connection reuse, TLS handshake per call). Two shared `SocketsHttpHandler` clients (normal / certificate-bypass) with
  connection pooling and DNS refresh; per-request timeouts; requests/responses disposed.
- **Redis (Bat.Cache.Redis):** values serialized/deserialized directly as UTF-8 bytes (same stored bytes); lock release/renew use
  `ScriptEvaluateAsync` (EVALSHA) and lock acquire uses typed `StringSetAsync(..., When.NotExists)` (raw-command fallback kept
  for `IRedisCacheProvider` mocks).
- **Crypto (Bat.Core):** derived keys cached (key derivation ran for every encrypted value, e.g. per EF converter call);
  one-shot `EncryptCbc/DecryptCbc`; crypto objects disposed — ~3× faster.
- **EF Core (Bat.EntityFrameworkCore):** `ApplyPersianYK`/`ApplyEnglishNumber` cache the string-property list per entity type and
  no longer scan/box numeric properties; `ToEnglishNumber` returns the input without allocating when it has no Persian/Arabic digits;
  `BasePropertiesInitializer` reads the clock once per SaveChanges.
- **Regex:** validators, validation attributes and mobile helpers use shared compiled `Regex` instances instead of `new Regex` per call.
- **Reflection:** `CopyFrom`/`UpdateWith`, `AuthorizationFilter`, Dapper TVP builder cache per-type metadata.
- **RabbitMQ:** `RabbitProducer` reuses one connection/channel instead of opening (and leaking) a connection per publish.
- Others: `MenuModel.ChildMenus` caches its deserialized value; `ToPagingListModel` enumerates once; `JwtService` shares one
  token handler; `ClientInfo` parses user agents without exceptions; large-file upload streams to disk; EPPlus license set once.

### Bug fixes
- `RepositoryFactory.Dispose/DisposeAsync` and `BulkRepositoryFactory.Dispose/DisposeAsync` recursed infinitely
  (StackOverflowException → process crash when a DI scope disposed them).
- `BatJwtParserMiddleware`: continued the pipeline after writing an error response; NullReferenceException when `IJwtService`
  was missing; caught **all downstream exceptions** and turned them into 401 responses (now only token validation is guarded).
- `BatExceptionHandlingMiddleware`: no longer tries to set status/headers after the response started; structured logging.
- `PersianDateTime.Now` added one hour during the first half of the year (Iran abolished DST in 2022).
- `PersianDateTime.GetMonthName(month)` used index `month + 1` (wrong name; threw for months 11–12).
- `FileLoger` used `\` separators (broken on Linux) and put the time (with `:`) in file names (one file per second; invalid on Windows).
  Now `<Kind>-yyyy-MM-dd.txt` via `Path.Combine`.
- `FileOperation.CreateDirectory` and upload paths in `HttpFileOperation` used `\` (broken on Linux/Kubernetes).
- `ValidatorExtensions.IsNationalCode` accepted most invalid codes (dangling `else`); `IsPicture` always returned false;
  `IsCarPlate` always returned false; `IsComplexPassword` threw with the default (null) config.
- `RegexPattern.PersianDate` never matched a real date (`\d{2,3,4}` is not a quantifier) → `IsPersianDate` and `[PersianDate]` always failed.
- `MobileNumberExtensions.ToStandardMobileNumber` always threw for numbers starting with `98`.
- `ObjectExtensions.GetProperty(object, name)` had an inverted null check (always null / NRE).
- `LogicExtensions.IsNotNull(Guid)` was always true; `ForEach(IAsyncEnumerable)` was `async void` (now returns `Task`).
- `ExpressionExtensions.Or` used bitwise `Or` instead of `OrElse`.
- `Randomizer.GetRandomString` only used the first `length` characters of the alphabet (and threw for length > 36);
  now uses a cryptographically secure generator (used for JWT refresh tokens).
- `BatNumberToStringConverter` formatted numbers with the current culture (fa-IR minus sign/decimal separator).
- `RedisCacheProvider`: `Set(KeyValuePair<string,string>[])`, `SetAsync(...)`, `Get(string[])`, `GetAsync(string[])` always threw
  (`ArrayTypeMismatchException`); `GetServer(host, port)` ignored the port; null `SslSettings` threw; methods failed when the
  provider was built from a multiplexer; `Set(pairs, flags)` ignored `flags`. Lock auto-renew now uses the configured `LockExpiry`.
- `DistributedLockResult.Dispose/DisposeAsync` never released the lock (`using`/`await using` kept it until expiry).
- `HttpRequestTools`: setting `Timeout` on a caller's `HttpClient` threw after its first request; `GetAsync<T>(url, mediaType)`
  mutated `DefaultRequestHeaders`; `PutFormAsync` never sent the given headers.
- `CorsExtensions.UseBatCors(domains, headers, methods)` applied an empty policy.
- `FileExtensions.SaveFile` / `HttpFileOperation.Save(byte[])` wrote nothing (threw); `SaveLargeFile` dereferenced a header before checking it.
- `AspNetCoreExtensions.FillWithHttpRequest` assigned raw `StringValues` (threw) and used a wrong key for query values.
- `ControllerExtensions.RenderViewToString` did not wait for rendering.
- `DbContextExtensions.GetAddedEntity/GetUpdatedEntity` returned Deleted entries; `ValidateContext` threw for two invalid
  entities of the same type; `SaveAuditLog` created rows for Unchanged entries and for audit rows themselves.
- `OrderBy(string)` threw for `"Name desc"`, multiple columns or nested paths (used by `ToPagingListDetailsAsync(…, orderBy)`).
- `DbContextFactory` created and leaked a context per call and ignored the connection string after the first call.
- `ParameterExtension.ToTableValuedParameter` threw for nullable properties; `DapperGenericRepo.GetPaging` ran the query twice when unbuffered.
- `RabbitProducer`: exchange name leaked between calls; `DisposeAsync` NRE; caller connections were closed. `RabbitConsumer.DisposeAsync` NRE.
- `Bat.Di`: one dynamic or unloadable assembly aborted the whole registration.
- `CurrentUserPrincipal.IsInRole` threw when `Roles` was null; `BiggerThanZero` threw for null.
- `RsaEncryptor` did not dispose `RSA`; `DistributedLock` sent the full Lua script on every call.

### Behavior changes (review when upgrading)
- `MemoryCacheProvider.Set(...)` now **overwrites** existing keys (it used `Add`, which kept the old value) and returns `true`.
- `BatJwtParserMiddleware` stops the pipeline after writing a 401/500 response; exceptions from later middlewares/controllers now
  reach the exception-handling middleware instead of being returned as 401.
- `IsNationalCode`, `IsPicture`, `IsCarPlate`, `IsPersianDate`, `[PersianDate]`, `Guid.IsNotNull()` now return correct results (some returned constant values before).
- `PersianDateTime.Now` is one hour earlier than 10.0.0 during Farvardin–Shahrivar (now correct).
- `FileLoger` writes one file per day per kind (`Info-1405-07-12.txt`) instead of one per second.
- `FileOperation.CheckExtension` is case-insensitive; `CreateDirectory` returns `true` for single-segment paths too.
- `ModifyDateSh` on modified entities now uses the server's local clock (`DateTime.Now`), like every other Insert/Modify value.
  It used Iran time (`PersianDateTime.Now`) before, so on servers running in UTC the stored value is now 3:30 earlier than in 10.0.0.
- `.heic` is accepted as an image by `IsPicture` and `FileOperation.CheckExtension(FileType.Image, …)`.
- `AuthorizationFilter` returns 401 (instead of throwing) when the user has no `NameIdentifier` claim.
- `RabbitProducer.Publish` without `exchangeName` always uses `Bat_Exchange`.
- Disposing a `DistributedLockResult` now actually releases the lock.

### Renames (old names kept as `[Obsolete]` aliases, so existing code still compiles)
- `FileOperation.CheckExtention` → `CheckExtension`.
- `RegexPattern.IrancellMobileNumber` → `IranCellMobileNumber`.
- `RedisSslSettings.Host` → `SslHost` (now matches the `SslHost` key the sample `appsettings.json` always used; `Host` still binds).
- Private static fields renamed to the `_camelCase` style (no public impact).

### Dependencies
- Pinned `Microsoft.OpenApi` 2.12.2 (Bat.AspNetCore) and `System.Security.Cryptography.Xml` 10.0.12 (Bat.Tools) to fix
  high-severity vulnerabilities (NU1903) in transitive versions.
- Removed a broken `readme.md` pack item from Bat.Dapper.

### Repository
- Added `AGENTS.md`, `docs/`, `CHANGELOG.md`, `Bat.NoSql.slnf` and `tests/Bat.Regression.Tests` (xUnit regression tests).
