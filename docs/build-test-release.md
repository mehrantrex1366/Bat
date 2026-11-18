# Build, test, release

## Prerequisites
- .NET SDK 11 (`global.json` pins `11.0.100` with `rollForward: latestFeature`, so any 11.0.x SDK works; prereleases are not allowed).
  The same `global.json` selects Microsoft.Testing.Platform for `dotnet test` (`"test": { "runner": "Microsoft.Testing.Platform" }`).
- Visual Studio on Windows only for `Bat.SqlClrAssembly.sqlproj` (SSDT).

## Build
```bash
dotnet build Bat.NoSql.slnf -c Release -p:GeneratePackageOnBuild=false
```
`Bat.NoSql.slnf` = every project in `Bat.sln` except the SQL CLR project. Building `Bat.sln` with the dotnet CLI fails
on that project (`MSB4278 ... SSDT ... targets`) — expected. If you add a project to `Bat.sln`, add it to `Bat.NoSql.slnf` too.

Single project: `dotnet build src/Core/Bat.Core/Bat.Core.csproj -c Release -p:GeneratePackageOnBuild=false`.

Expected result: 0 warnings, 0 errors. There are no security pins at the moment (10.0.14 removed them: the parent packages
now bring patched versions). Don't reference packages that the shared framework already contains: the SDK reports them
as `NU1510` (11.0.0 removed `System.ComponentModel.Annotations` and `Microsoft.Extensions.Options` for that reason). If a new `NU1903` warning appears, pin the patched version in the "Security pins" section of
`Directory.Packages.props` (see [runbook](../Specs/central-package-management/runbook.md)), and check
`dotnet list Bat.NoSql.slnf package --vulnerable --include-transitive` after every upgrade.

## Build files (Central Package Management)
| File | Holds |
|---|---|
| `Directory.Packages.props` | Every NuGet package version (`<PackageVersion>`). csproj files use `<PackageReference Include="X" />` without a version. |
| `Directory.Build.props` | All projects: `TargetFramework`, `ImplicitUsings`, Source Link / symbols / deterministic settings. |
| `src/Directory.Build.props` | Published packages: shared `Version`, authors, company, copyright, repository, `IsPackable`. |
| `src/Directory.Build.targets` | Published packages: `Bat.png` icon and `readme.md` when the file exists next to the csproj. |

A csproj keeps only what is specific to it (Description, PackageTags, PackageReleaseNotes, references, resources).
Full rules and rationale: [../Specs/central-package-management/](../Specs/central-package-management/README.md).

## Test
```bash
dotnet test Bat.NoSql.slnf -c Release -p:GeneratePackageOnBuild=false   # or: dotnet test --project tests/Bat.Regression.Tests
```
`tests/Bat.Regression.Tests` is an **xUnit v3** project (`OutputType=Exe`) run by **Microsoft.Testing.Platform** (selected in
`global.json`; the old VSTest mode fails with "Testing with VSTest target is no longer supported"). It also runs on its own:
`dotnet run --project tests/Bat.Regression.Tests -c Release`. Use `TestContext.Current.CancellationToken` for calls that accept a
token (analyzer `xUnit1051`). The project uses `Microsoft.NET.Sdk` + `FrameworkReference Microsoft.AspNetCore.App`; don't switch it
to `Microsoft.NET.Sdk.Web` (the .NET 11 static web assets targets fail on it).

It needs no network, database, Redis or RabbitMQ:
- `CoreTests.cs` — JSON conventions, shared options, known ciphertexts (data compatibility!), validators, Persian date/time, misc fixes.
- `DataAccessTests.cs` — `BatDbContext` SaveChanges pipeline (EF InMemory), changed-entity helpers, dynamic `OrderBy`, factories, Dapper TVP, memory cache.
- `HttpTests.cs` — `HttpRequestTools` against an in-process Kestrel server (headers, cookies, timeouts, PUT form), `ClientInfo` probe user agents.
- `ClientInfoTests.cs` — User-Agent detection (~80 real user agents: OS, browser, engine, device type, manufacturer/model), client hints, hostile input.
- `SwaggerTests.cs` — real `swagger.json` from a local server with `AddBatSwagger` + all Bat Swagger filters (Bearer requirement, `Token` header, exclusions).
- `PlatformTests.cs` — Bat.Di auto-registration (lifetimes, open generics), JWT create/validate (JWE), `AddBatHybridCache` (in-memory HybridCache), EF encryptor value converters (pinned ciphertext).
- `Net11FeatureTests.cs` — HybridCache (Bat JSON in L2, two pods sharing L2, options/state/context overloads, 10.x call shapes, metrics,
  Redis registration without a server), zstd, JSON Lines, async validation (also through MVC), settings validators, AuthenticatedEncryption
  (incl. RFC 3394 vector), X25519, ProcessTools, health probes (short-circuit, live vs ready) and zstd response compression.
- `TestSetup.cs` — module initializer that bypasses `HTTP_PROXY`/`HTTPS_PROXY` for loopback, so the local-server tests work on machines with a proxy.

`HttpTests.cs` and `SwaggerTests.cs` call `http://127.0.0.1`; `TestSetup.cs` makes loopback bypass any configured proxy
(before 11.0.0 you had to add `localhost,127.0.0.1` to `NO_PROXY`).

Not covered by automated tests (need infrastructure): Redis provider/lock, HybridCache with a Redis L2, RabbitMQ, SQL Server specifics
(incl. EFCore.BulkExtensions), tag helpers, `RedisHealthCheck` and `AddBatHybridCacheWithRedis` against a live Redis.

Public API compatibility: before a release, compare every package with the previous version using Microsoft ApiCompat
(`dotnet tool install Microsoft.DotNet.ApiCompat.Tool`, then `apicompat -l old/Bat.X.dll -r new/Bat.X.dll` with
`--left-assembly-references` / `--right-assembly-references`). For 11.0.0 all 12 packages reported no breaking change against 10.0.14.
(Package validation with `PackageValidationBaselineVersion` can't compare across the dropped `net10.0` TFM: it stops at `PKV006`.)
For 10.0.1 and 10.0.2 the Redis provider + distributed lock were verified manually against a local `redis-server`, and the RabbitMQ
producer/consumer against a local `rabbitmq-server` (200 concurrent publishes over one connection). Do the same when you
change those parts and describe what you verified.

`src/Test/Bat.Test` is **not** a test project; it is a published helper library for consumers' unit tests.

## Versioning
- All packages share one version: `<Version>` in `src/Directory.Build.props` (currently `11.0.0`).
- Major = .NET major (11). Patch for fixes/perf, minor for new APIs.
- Any change → bump the version there (it applies to **all** packages) + entry in `CHANGELOG.md`.

## Pack / publish
```bash
dotnet pack Bat.NoSql.slnf -c Release -p:ContinuousIntegrationBuild=true -o ./artifacts
```
This produces 12 packages (`.nupkg` + `.snupkg` each): every `src/**` project except the SQL CLR one.
Packages include `Bat.png` and the package `readme.md` (`PackageReadmeFile`) when the project has them; `Bat.Queue` has no
readme and `Bat.Test` has no icon. `ContinuousIntegrationBuild=true` makes the build deterministic (normalized source paths)
for Source Link; don't set it for local debugging builds. Pack from a pushed commit: Source Link points at the current commit.
Expected pack warning: `NU5104` on Bat.Di (depends on the prerelease `DryIoc.Microsoft.DependencyInjection`, the only published version).
Publishing (NuGet feed and API key) is done by the owner; agents must not publish packages.
Publish only the `.nupkg` files produced by this command. Don't build packages by hand (e.g. NuGet Package Explorer
or an old `.nuspec`): 10.0.12 was published that way with the 10.0.2 DLLs and stale dependencies
(`StackExchange.Redis >= 2.10.1` for a DLL compiled against 3.x → `CS1705` in consumers).
Before pushing, check: `unzip -p artifacts/Bat.Cache.Redis.<ver>.nupkg "*.nuspec"` shows the versions from `Directory.Packages.props`.

## After releasing
Consumers must update **all** Bat packages to the same version (e.g. a service that uses Bat.Cache.Redis and Bat.Http
must not mix 10.0.1 and 10.0.2), otherwise an older Bat.Core may be resolved.
