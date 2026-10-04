# AGENTS.md — Bat Framework

Instructions for AI agents and new contributors working in this repository.
Read this file first, then only the `docs/` pages relevant to your task. **You should not need to read
the whole solution**: `docs/reference/<Package>.md` describes every package's public surface, behavior and pitfalls.

## What this repository is

Bat is a set of NuGet libraries (all `net10.0`, C# latest, version `10.0.x`) that the owner's team uses as the
shared foundation of several internal products (CRM, payment gateway, link shortener, bulk messaging, ...).
Those products are ASP.NET Core microservices (DDD, Clean/Onion), mostly hosted on **Kubernetes (Linux)**;
the payment gateway runs on **Windows servers**. Code here therefore must work on both Linux and Windows,
and it runs on hot request paths of production services: **performance and backward compatibility matter more
than elegance.**

| Package | Folder | One-line purpose | Doc |
|---|---|---|---|
| Bat.Core | `src/Core/Bat.Core` | Extensions, JSON, Persian date/number/text helpers, crypto, validation, paging, response pattern, DI marker interfaces | [docs/reference/Bat.Core.md](docs/reference/Bat.Core.md) |
| Bat.Tools | `src/Core/Bat.Tools` | Excel export (EPPlus) | [docs/reference/Bat.Tools.md](docs/reference/Bat.Tools.md) |
| Bat.Cache | `src/Cache/Bat.Cache` | In-process `MemoryCacheProvider` (System.Runtime.Caching) | [docs/reference/Bat.Cache.md](docs/reference/Bat.Cache.md) |
| Bat.Cache.Redis | `src/Cache/Bat.Cache.Redis` | Redis provider (StackExchange.Redis), distributed lock, HybridCache wrapper | [docs/reference/Bat.Cache.Redis.md](docs/reference/Bat.Cache.Redis.md) |
| Bat.Http | `src/AspNetCore/Bat.Http` | Static HTTP client helpers, client/device info from User-Agent | [docs/reference/Bat.Http.md](docs/reference/Bat.Http.md) |
| Bat.AspNetCore | `src/AspNetCore/Bat.AspNetCore` | JWT, authorization filter, middlewares, Swagger, CORS, file upload, tag helpers | [docs/reference/Bat.AspNetCore.md](docs/reference/Bat.AspNetCore.md) |
| Bat.Di | `src/AspNetCore/Bat.Di` | Auto-registration by marker interface (MS DI, Autofac, DryIoc) | [docs/reference/Bat.Di.md](docs/reference/Bat.Di.md) |
| Bat.EntityFrameworkCore | `src/DataAccess/Bat.EntityFrameworkCore` | `BatDbContext`, generic repository, paging, audit log, SaveChanges pipeline | [docs/reference/Bat.EntityFrameworkCore.md](docs/reference/Bat.EntityFrameworkCore.md) |
| Bat.EntityFrameworkCore.Tools | `src/DataAccess/Bat.EntityFrameworkCore.Tools` | Bulk repo (EFCore.BulkExtensions), value converters, model-builder helpers | [docs/reference/Bat.EntityFrameworkCore.Tools.md](docs/reference/Bat.EntityFrameworkCore.Tools.md) |
| Bat.Dapper | `src/DataAccess/Bat.Dapper` | Dapper extensions, TVP helpers, generic Dapper repo | [docs/reference/Bat.Dapper.md](docs/reference/Bat.Dapper.md) |
| Bat.Queue | `src/Queue/Bat.Queue` | RabbitMQ producer/consumer (RabbitMQ.Client 7), MSMQ interface | [docs/reference/Bat.Queue.md](docs/reference/Bat.Queue.md) |
| Bat.Test | `src/Test/Bat.Test` | Test **helpers** for consumers (Moq/NSubstitute/Bogus builders). Not a test project. | [docs/reference/Bat.Test.md](docs/reference/Bat.Test.md) |
| Bat.SqlClrAssembly | `src/Sql/Bat.SqlAssembly` | SQL Server CLR functions (.NET Framework, SSDT). Windows/Visual Studio only | [docs/reference/Bat.SqlClrAssembly.md](docs/reference/Bat.SqlClrAssembly.md) |
| Bat.Regression.Tests | `tests/Bat.Regression.Tests` | xUnit regression tests for this repo | [docs/build-test-release.md](docs/build-test-release.md) |

Dependency graph and cross-cutting design: [docs/architecture.md](docs/architecture.md).

## Build and test (do this before and after every change)

```bash
dotnet build Bat.NoSql.slnf -c Release -p:GeneratePackageOnBuild=false   # everything except the SQL CLR project
dotnet test  Bat.NoSql.slnf                                              # regression tests (local only, no network/DB needed)
```

- `Bat.sln` also contains `Bat.SqlClrAssembly.sqlproj`, which only builds in Visual Studio on Windows (SSDT).
  `dotnet build Bat.sln` fails on that project by design — use `Bat.NoSql.slnf`.
- Several projects have `GeneratePackageOnBuild=true`; pass `-p:GeneratePackageOnBuild=false` unless you want `.nupkg` files.
- The build must stay at **0 errors and 0 warnings** (including `NU1903` vulnerability warnings).
- Never run `git clean -x` in a working copy you don't own: it deletes untracked work (docs, new files) and local settings.

## Hard rules

1. **Do not break the public API.** These packages are consumed by many services. Do not remove public types,
   members or parameters, and do not change return types. Add overloads or new members instead; renames need an `[Obsolete]` alias.
   If a behavior must change (bug fix), document it in `CHANGELOG.md` under "Behavior changes".
2. **Never change encryption output.** `Encryption`, `AesEncryption`, `AesAlgorithm`, `RijndaelAlgorithm`,
   `HashGenerator` and the EF value converters (`StringEncryptorConverter`, `IntEncryptorConverter`, `GuidEncryptorConverter`)
   protect data that is already stored in databases. Ciphertexts must stay byte-for-byte identical
   (`tests/Bat.Regression.Tests/CoreTests.cs` pins known values). Weak defaults are documented in `docs/known-issues.md`; don't "fix" them silently.
3. **Keep the JSON contract.** `SerializationExtension` defaults (camelCase, case-insensitive, `IncludeFields`,
   `IgnoreCycles`, relaxed escaping, numbers-from-strings, Bat converters) are relied on by every consumer
   (Redis payloads, HTTP bodies, DB columns). Do not change them.
4. **Follow the performance rules** in [docs/conventions.md](docs/conventions.md). The most important:
   - never create `JsonSerializerOptions` per call — use `SerializationExtension.DefaultOptions` / `GetSharedOption(depth)` or the `SerializeToJson`/`DeSerializeJson` helpers;
   - never create `HttpClient`/`HttpClientHandler` per call;
   - never `new Regex(...)` per call (use `BatRegex` in Bat.Core or a `static readonly` instance);
   - cache reflection results (`GetProperties`, `GetCustomAttributes`) per type;
   - no `async void`, no sync-over-async in new code, dispose `IDisposable`s.
5. **Cross-platform paths.** Use `Path.Combine` / `Path.DirectorySeparatorChar`. Never hard-code `\\`.
6. **Culture.** Servers may run with `fa-IR` culture. Format/parse machine-readable numbers with `CultureInfo.InvariantCulture`.
   "Iran time" is `PersianDateTime.Now` (UTC+03:30, no DST since 2022); `DateTime.Now` is the server's local time (often UTC on Kubernetes).
7. **Versioning.** All packages share one version. When you change any package, bump `<Version>` in **every**
   `src/**/*.csproj` (consumers reference them together) and add an entry to `CHANGELOG.md`.
8. **Tests.** Every bug fix gets a regression test in `tests/Bat.Regression.Tests` when it can run without external services.

## Code conventions (short version — full list in docs/conventions.md)

- Flat namespaces per package: `Bat.Core`, `Bat.Http`, `Bat.AspNetCore`, `Bat.Cache.Redis`, `Bat.EntityFrameworkCore`, ...
  (exceptions: `ExpressionExtensions` is in `Bat.Tools` namespace inside Bat.Core; `HybridCacheProvider` is in `Bat.Cache.Hybrid`).
- Each project has `GlobalUsing.cs`; check it before adding `using`s.
- File-scoped namespaces, 4-space indentation, `_camelCase` for private fields **and** private static fields (owner's style; no `s_` prefix).
- Existing misspellings are part of the public API (`FileLoger`, `Extentions` folders, `PagingExtention`, `byPassServerSertificate`, `commandTimOut`, ...).
  Don't rename them casually; if a public name is renamed, keep the old name as an `[Obsolete]` alias that forwards to the new one
  (as done for `CheckExtention` → `CheckExtension`, `IrancellMobileNumber` → `IranCellMobileNumber`, `RedisSslSettings.Host` → `SslHost`).
- Files in git are stored with LF; Windows checkouts use CRLF (`core.autocrlf`). Many `.cs` files start with a UTF-8 BOM — preserve it.
- Comments explaining *why* a non-obvious fix exists are welcome (several fixes are annotated with `// Fixed:`).

## Where to look

| Task | Read |
|---|---|
| Understand structure / dependencies | [docs/architecture.md](docs/architecture.md) |
| Change or use a package | `docs/reference/<Package>.md` |
| Style, performance and safety rules | [docs/conventions.md](docs/conventions.md) |
| Build, test, pack, version, publish | [docs/build-test-release.md](docs/build-test-release.md) |
| Known bugs / tech debt not fixed yet | [docs/known-issues.md](docs/known-issues.md) |
| What changed and why | [CHANGELOG.md](CHANGELOG.md) |

Keep these docs up to date in the same change when you alter a package's behavior or public surface.
