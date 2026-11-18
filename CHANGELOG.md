# Changelog

All packages share one version. Newest first.

## 11.0.0 — .NET 11 (2026-11)

All packages now target **`net11.0`** (was `net10.0`) and require the .NET 11 SDK / runtime. Every public type and member of 10.0.14
is still there with the same signature; encryption output and JSON defaults are unchanged (pinned by tests).
Upgrade guide for services: [docs/migration-to-11.md](docs/migration-to-11.md).

### Dependencies (all packages)
- Microsoft 10.0.15 → **11.0.0**: EF Core (+ SqlServer, InMemory, Tools), Microsoft.AspNetCore.Authentication.JwtBearer,
  Microsoft.Extensions.Caching.StackExchangeRedis, Configuration, Options, Http.Polly, System.Runtime.Caching;
  Microsoft.Extensions.Caching.Hybrid 10.11.0 → **11.0.0**.
- **Swashbuckle.AspNetCore 10.3.1 → 11.0.1** (Microsoft.OpenApi 2.x → **3.11**). Microsoft.OpenApi types are part of
  Bat.AspNetCore's public API (`AddBatSwagger(OpenApiInfo, ...)`, the Swagger filters) — consumers' own Swagger filters may need changes.
- MockQueryable.Moq / .EntityFrameworkCore 10.0.13 → **11.0.0** (Bat.Test).
- Already latest and kept: Autofac.Extensions.DependencyInjection 11.0.2, Dapper 2.1.89, Microsoft.Data.SqlClient 7.1.2,
  StackExchange.Redis 3.4.35, RabbitMQ.Client 7.2.2, EPPlus 8.7.2, Polly 8.9.1, NSubstitute 6.2.0, Moq 4.21.0.
- **EFCore.BulkExtensions.SqlServer stays on 10.0.1**: no EF Core 11 build is published yet. It compiles and restores against
  EF Core 11 but was not verified on SQL Server (see known issues).
- Not adopted: prereleases (StackExchange.Redis 4 alpha, SqlClient 8 preview, EPPlus 9 preview, RabbitMQ.Client 7.3 beta,
  AutoFixture 5 rc, DryIoc.Microsoft.DependencyInjection is still `8.0.0-preview-04` → `NU5104` on pack as before).
- Removed redundant references that .NET 11 reports as `NU1510` (already part of the shared framework):
  `System.ComponentModel.Annotations` (Bat.Dapper) and `Microsoft.Extensions.Options` (Bat.Queue). The types still come from the framework.
- New dependency: Bat.Cache.Redis → `Microsoft.Extensions.Diagnostics.HealthChecks` 11.0.0 (for `AddBatRedisHealthCheck`).
- `dotnet list package --vulnerable --include-transitive`: no vulnerable packages.

### New
- `services.AddBatHybridCache(Action<HybridCacheOptions> = null)` (Bat.Cache.Redis, namespace `Bat.Cache.Hybrid`): registers
  `HybridCache` and `IHybridCacheProvider`. Before, `IHybridCacheProvider` could not be resolved at all (internal implementation).
- `IHybridCacheProvider.GetOrSetAsync(key, factory, expiration)` and `GetOrSetAsync(key, factory, expiration, ct)`: the same call
  did not compile before (CS0121, ambiguous between the flags and tags overloads). `IHybridCacheProvider.DeleteByTagAsync(tag, ct)`.
  All three are default interface members, so existing implementations/mocks keep compiling.
- `BatSwaggerAuthorizeFilter.BearerSchemeName` (`"Bearer"`).

**HybridCache (Bat.Cache.Redis)**
- L2 values are written with Bat's JSON contract (`BatHybridCacheSerializerFactory`, registered by `AddBatHybridCache`): the HybridCache
  default wrote PascalCase and silently dropped public fields.
- `GetOrSetAsync(key, factory, HybridCacheEntryOptions, tags, ct)` (flags **and** tags together), `GetOrSetWithStateAsync` (no closure),
  `GetOrSetWithContextAsync` (.NET 11 `HybridCacheEntryContext`), `SetAsync`. Default interface members with distinct parameter types
  or names, so no existing call becomes ambiguous (a test compiles the 10.x call shapes).
- `AddBatHybridCacheWithRedis(instanceName, configure)`: Redis L2 on the connection of `IRedisCacheProvider` (no second multiplexer).
- `AddBatCacheStatistics()`: .NET 11 `MemoryCacheOptions.TrackStatistics` (`dotnet.cache.*` metrics).
- Hit/miss metrics: `BatCacheMetrics` (Bat.Core) — meter `Bat.Cache`, counter `bat.cache.requests`; recorded by `IHybridCacheProvider`
  and `MemoryCacheProvider.Get/GetCacheItem` (no cost without a listener).

**.NET 11 features (all opt-in, nothing changes until a service calls them)**
- Bat.Core: `CompressionTools` (zstd, `ZstandardStream`), `JsonLinesExtensions` (`SerializeAsyncEnumerable`, Bat JSON),
  `DataAnnotationsValidationExtensions` (async DataAnnotations), `ProcessTools.RunAsync` (`Process.RunAndCaptureTextAsync`),
  `AuthenticatedEncryption` (AES-256-GCM + AES Key Wrap `Aes.EncryptKeyWrap`, for **new** data only), `X25519KeyAgreement`.
- Bat.AspNetCore: `AddBatHealthChecks` / `MapBatHealthChecks` (live/ready probes with `ShortCircuit`), `AddBatResponseCompression` /
  `UseBatResponseCompression` (zstd + Brotli + gzip), `BatAsyncValidationAttribute`, `JwtSettingsValidator` + `ValidateBatJwtSettings()`.
- Bat.Cache.Redis: `RedisHealthCheck` + `AddBatRedisHealthCheck()`, `RedisSettingsValidator` + `ValidateBatRedisSettings()`.

### Fixes
- `BatSwaggerAuthorizeFilter` created `new OpenApiSecuritySchemeReference("")`; Microsoft.OpenApi 3 throws on the empty id, so
  `swagger.json` returned **500** as soon as one action had `[Authorize]`. It now references the document's `Bearer` scheme.
- `BatSwaggerExcludeFilter` removed `[SwaggerExclude]` properties by their CLR name (`Secret`) while schema keys are camelCase (`secret`),
  so nothing was excluded. Names are now matched case-insensitively; reflection results are cached per type.

### Behavior changes (review when upgrading)
- `net11.0` only: services must move to .NET 11 (SDK, Docker images, Windows Hosting Bundle) together with Bat 11.
- Swagger: properties marked `[SwaggerExclude]` now disappear from the generated schemas (they were visible before).
- Swagger: `[Authorize]` actions reference the `Bearer` security scheme; add `AddSecurityDefinition("Bearer", ...)` (see Bat.AspNetCore docs).

### Build and tests
- `global.json`: SDK `11.0.100` (`rollForward: latestFeature`, no prereleases) and `"test": { "runner": "Microsoft.Testing.Platform" }`.
- Tests moved to **xUnit v3** (`xunit.v3` 4.0.3, Microsoft.Testing.Platform; `OutputType=Exe`) and from `Microsoft.NET.Sdk.Web` to
  `Microsoft.NET.Sdk` + `FrameworkReference Microsoft.AspNetCore.App` (the .NET 11 static web assets targets fail on a Web-SDK test project).
- New tests (135 → 172): Swagger setup and filters against a real `swagger.json`, Bat.Di auto-registration, JWT round trip / rejection,
  `AddBatHybridCache` (in-memory), EF encryptor value converters (pinned ciphertext). Loopback requests bypass `HTTP_PROXY`
  automatically (`TestSetup.cs`), so the HTTP tests no longer need `NO_PROXY` on developer machines.
  `Net11FeatureTests.cs` covers every feature above; `CoreTests.CiphertextsAndHashes_FromNet10_StillMatchOnNet11` pins values produced on .NET 10.

### Compatibility verification
- Microsoft ApiCompat, each of the 12 packages 11.0.0 vs 10.0.14: **no breaking change** in the public API.
- Crypto: 270 ciphertexts/hashes generated by Bat 10.0.14 on .NET 10.0.15 and by 11.0.0 on .NET 11.0.0 are byte-for-byte identical; each side
  decrypts the other's values and validates the other's JWE tokens and RSA signatures (Windows; algorithms are standard, Linux not run).

## 10.0.14 — Bat.Http: User-Agent detection rewrite (2026-10)

`ClientInfo` user-agent parsing was rewritten (`src/AspNetCore/Bat.Http/HttpRequestTools/UserAgentParser.cs`, internal).
No public member was removed or changed; all existing `ClientInfo` methods keep their signatures and now use the new parser.

New:
- `ClientInfo.ParseUserAgent(string userAgent)` and `ParseUserAgent(string userAgent, IHeaderDictionary headers)` (never return `null`).
- `RequestDetails.DeviceType` (`Desktop`, `Mobile`, `Tablet`, `TV`, `Console`, `Wearable`, `Bot`, `Unknown`), `IsTablet`, `IsBot`,
  `EngineName` (`Blink`, `WebKit`, `Gecko`, `Trident`, `EdgeHTML`, `Presto`, ...) and `EngineVersion`.
- **User-Agent Client Hints** (`Sec-CH-UA*`, Chromium browsers over HTTPS) are used by `GetRequestDetails(HttpContext)`:
  Windows 11 vs 10, real macOS / Android versions, the device model and the full browser version that Chrome hides in its
  reduced User-Agent, and Brave. Send `Accept-CH: ClientInfo.AcceptClientHints` in responses to receive the high-entropy hints.
- Detected browsers: Chrome, Edge (Chromium, legacy, Android, iOS), Firefox (+ iOS, Focus, Klar), Safari, Opera (+ Mini, Mobile, Touch, iOS),
  Internet Explorer / IE Mobile, Samsung Internet, Yandex, UC, MIUI, Huawei, HeyTap, Oppo, Vivo, Silk, DuckDuckGo, QQ, Brave, Vivaldi,
  Whale, Coc Coc, Maxthon, Puffin, Avast/AVG, Chromium, Chrome Headless, Electron, SeaMonkey, Waterfox, Pale Moon, GNOME Web, Konqueror, ...;
  Android WebView, iOS WebView and the in-app browsers of Instagram, Facebook, TikTok, Snapchat, Pinterest, LinkedIn, Twitter, WeChat, Line, Google App;
  bots (Googlebot, Bingbot, Yandex, Baidu, social preview fetchers, AI crawlers, SEO crawlers, uptime monitors, kube-probe, generic `*bot/`, `crawler`, `spider`);
  tools (Postman, curl, Wget, PowerShell, OkHttp, Python, Go, Java, axios, Dart, Dalvik, CFNetwork, ...).
- Detected operating systems: Windows 95 to 11, Windows Phone / Mobile, macOS, iOS / iPadOS (incl. the iOS 26 frozen-version fix), watchOS, tvOS,
  Android, HarmonyOS, Fire OS, Chrome OS, Linux and distributions (Ubuntu, Fedora, Debian, ...), FreeBSD / OpenBSD / NetBSD, Solaris,
  BlackBerry, Symbian, KaiOS, Tizen, webOS, Sailfish, Firefox OS, Chromecast, Roku, Xbox, PlayStation, Nintendo.
- Device manufacturer and model for Apple, Samsung, Google, Xiaomi, Huawei/Honor, OPPO, Realme, vivo, OnePlus, Motorola, LG, Nokia, Sony,
  Amazon and others (from the User-Agent model code or `Sec-CH-UA-Model`).

### Behavior changes (review when upgrading)
Existing OS / browser names are kept (`IOS`, `Mac OS`, `Black Berry`, `Windows 10`, `SamsungBrowser`, `UCBrowser`, `Postman`, `Unknown`, ...)
so stored values and reports stay comparable. What changes:
- Browsers that were reported as `Chrome` are now detected correctly: Edge (`Edg/`), Opera (`OPR/`), Samsung Internet, Yandex, MIUI, Instagram, ...
- **Safari version** is now the real version (`Version/17.4.1`); it was the WebKit build number (`604.1`).
- Previously `Unknown` values are now detected: Internet Explorer, Linux desktops, Chrome OS, iPod, bots, TVs, consoles, ... (see lists above).
- macOS `OsVersion` is `10.15.7` (frozen by browsers; the real version comes from client hints) instead of empty.
- Windows Phone 10 is `Windows Phone` (was `Android`). Kindle / Fire devices are `Fire OS` (were `Android` or `Linux`).
- With client hints, Windows 11 is reported as `Windows 11` (the User-Agent alone still says `Windows 10`).
- `Manufacture` is the device maker (`Apple`, `Samsung`, `Xiaomi`, ... or `Unknown`) instead of `Microsoft Windows 10` / `Apple IOS`;
  `Model` is the device model (`SM-S911B`, `iPhone`, `Pixel 5`, ...) instead of the last segment inside `(...)` (e.g. ` x64`).
  `DeviceLog.Device` changes accordingly. `GetRequestDeviceManufacture(os)` itself is unchanged.
- `IsMobile` is `true` for phones, tablets and wearables (Android tablets were `false`; iPads were already `true`) and `false` for bots
  (Googlebot's smartphone User-Agent was `true`).
- `GetRequestOs` / `GetRequestBrowser` / `GetRequestDeviceModel` accept `null` (treated as an empty User-Agent) instead of throwing.
- User agents longer than 1024 characters are parsed on their first 1024 characters only.

### Dependencies (all packages)
Every dependency moved to its latest version that supports `net10.0`; no source change was needed.
- Microsoft 10.0.x servicing → **10.0.15**: EF Core (+ SqlServer, InMemory, Tools), JwtBearer, Microsoft.Extensions.Caching.StackExchangeRedis,
  Configuration, Options, Http.Polly, System.Runtime.Caching. Microsoft.Extensions.Caching.Hybrid 10.10.0 → **10.11.0**,
  Microsoft.AspNetCore.Http.Abstractions 2.3.13 → **2.3.14**.
- Microsoft.Data.SqlClient 7.1.1 → **7.1.2**, StackExchange.Redis 3.3.1 → **3.4.35**, Swashbuckle.AspNetCore 10.2.3 → **10.3.1**,
  EPPlus 8.7.1 → **8.7.2**, Polly 8.8.0 → **8.9.1**, MockQueryable.Moq / .EntityFrameworkCore 10.0.12 → **10.0.13**.
- Test tooling: Microsoft.NET.Test.Sdk 18.12.0, xunit.runner.visualstudio 4.0.1.
- **Security pins removed**: `System.Security.Cryptography.Pkcs` (Bat.Dapper, Bat.EntityFrameworkCore, Bat.EntityFrameworkCore.Tools),
  `System.Security.Cryptography.Xml` (Bat.Tools) and `Microsoft.OpenApi` (Bat.AspNetCore) are no longer direct dependencies.
  The upgraded parent packages bring patched versions; `dotnet list package --vulnerable --include-transitive` reports nothing.
- Not adopted on purpose: the 11.0 packages (EF Core 11 / MockQueryable 11 need .NET 11; the rest belong to the .NET 11 release train)
  and Swashbuckle.AspNetCore 11 (moves to Microsoft.OpenApi 3, a breaking change for consumers' own Swagger filters and for
  `AddBatSwagger(OpenApiInfo, ...)`). Prereleases (StackExchange.Redis 4 alpha, SqlClient 8 preview, EPPlus 9 preview) are not used.

## 10.0.13 — republish of 10.0.12 (2026-10)

No source change. The 10.0.12 packages on nuget.org were not produced by `dotnet pack`: they contain the **10.0.2**
assemblies (`AssemblyVersion 10.0.2.0`) with a hand-edited, outdated nuspec. For Bat.Cache.Redis the nuspec declared
`StackExchange.Redis >= 2.10.1` while the DLL was compiled against 3.3.1, so consumers got
`CS1705: ... uses 'StackExchange.Redis, Version=3.0.0.0' which has a higher version than referenced assembly ... Version=2.0.0.0`.
10.0.13 is packed with `dotnet pack` from the repository (dependencies come from `Directory.Packages.props`).
Unlist 10.0.12 on nuget.org and update consumers to 10.0.13 (all Bat packages together).

Security pins (new advisories GHSA-846f-f764-76mx, GHSA-2fmv-rv2c-59x7, GHSA-7hqp-7c5m-4vq6, GHSA-c399-9853-6jmj):
- `System.Security.Cryptography.Xml` 10.0.12 → **10.0.13** (Bat.Tools, via EPPlus).
- `System.Security.Cryptography.Pkcs` pinned to **10.0.13** (was 9.0.x transitively via Microsoft.Data.SqlClient) in
  Bat.Dapper, Bat.EntityFrameworkCore and Bat.EntityFrameworkCore.Tools.

## 10.0.12 — Central Package Management and shared build files (2026-10)

Build-only change: no source code, public API or dependency version changed. Spec: [Specs/central-package-management/](Specs/central-package-management/README.md).

- Package version jumps from 10.0.2 to **10.0.12** to line up with the .NET 10.0.12 servicing release the packages are built on.
- Repository address is now **<https://github.com/mehrantrex1366/Bat>** (`RepositoryUrl`, Bat.EntityFrameworkCore `PackageProjectUrl`,
  root README); it was `mehrannoruzi/Bat`. It now matches the git remote, so Source Link and the nuspec point to the same repository.
- All NuGet versions moved to `Directory.Packages.props` (CPM); `<PackageReference>`s no longer carry `Version=`.
- Shared settings moved out of the csproj files: `Directory.Build.props` (target framework, implicit usings, Source Link,
  `snupkg` symbols, deterministic) and `src/Directory.Build.props` / `src/Directory.Build.targets` (version, authors, company,
  copyright, repository, icon, readme). The package version is now set in one place.
- `RepositoryType` is `git` for every package (was `GitHub` / `Microsoft DevOps Server`); Bat.Queue now has a `RepositoryUrl`.

Packaging fixes (`dotnet pack Bat.NoSql.slnf` now produces all 12 packages):
- **Bat.AspNetCore** was not packable (`Microsoft.NET.Sdk.Web` defaults to `IsPackable=false`); now packed. Its
  `appsettings.json` is excluded from the package.
- **Bat.Tools** failed to pack (`NU5046`: icon declared but `Bat.png` not included); now includes the icon.
- **Bat.Test** was packed as `1.0.0` with authors `Bat.Test`; now uses the shared version, metadata and its `readme.md`.

Apart from version and repository URL, the other 9 packages are identical to before (nuspec and file list compared). Assembly attribute change only:
Bat.Dapper's `AssemblyCompany` is now `Mehran Norouzi` (was empty → defaulted to `Bat.Dapper`).

## 10.0.2 — dependency updates (2026-10)

All NuGet dependencies updated to their latest stable versions. No code changes; build has 0 warnings, all regression tests
pass, and Redis (provider, distributed lock, `IDistributedCache`, `HybridCache`), RabbitMQ (200 concurrent publishes) and the
Bat.Test helpers were re-verified against real local services.

| Package | From | To |
|---|---|---|
| **StackExchange.Redis** | 2.12.14 | **3.3.1 (major)** |
| **NSubstitute** (Bat.Test) | 5.3.0 | **6.2.0 (major)** |
| xunit.runner.visualstudio (tests only) | 3.1.5 | 4.0.0 |
| Microsoft.EntityFrameworkCore / .SqlServer / .Tools / .InMemory | 10.0.7 | 10.0.12 |
| Microsoft.AspNetCore.Authentication.JwtBearer | 10.0.7 | 10.0.12 |
| Microsoft.Extensions.Caching.StackExchangeRedis, Configuration, Options, Http.Polly; System.Runtime.Caching | 10.0.7 | 10.0.12 |
| Microsoft.Extensions.Caching.Hybrid | 10.5.0 | 10.10.0 |
| Swashbuckle.AspNetCore | 10.1.7 | 10.2.3 |
| Microsoft.Data.SqlClient | 7.0.1 | 7.1.1 |
| Dapper | 2.1.72 | 2.1.89 |
| EPPlus | 8.5.4 | 8.7.1 |
| RabbitMQ.Client | 7.2.1 | 7.2.2 |
| Autofac.Extensions.DependencyInjection | 11.0.0 | 11.0.2 |
| Polly | 8.6.6 | 8.8.0 |
| Microsoft.AspNetCore.Http.Abstractions | 2.3.9 | 2.3.13 |
| Moq | 4.20.72 | 4.21.0 |
| MockQueryable.Moq / .EntityFrameworkCore | 10.0.5 | 10.0.12 |

Intentionally not upgraded:
- `Microsoft.OpenApi` stays on 2.12.2 (latest 2.x). 3.x is a breaking major and Swashbuckle 10.2.3 is built against 2.x.
- `DryIoc.Microsoft.DependencyInjection` 8.0.0-preview-04 is already the latest published version.

Consumers: services that reference StackExchange.Redis or NSubstitute directly should move to the same majors, or NuGet
will raise a downgrade error (NU1605).

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
