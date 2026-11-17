# Build, test, release

## Prerequisites
- .NET SDK 10 (`global.json` pins `10.0.100-rc.2` with `rollForward: latestFeature`, `allowPrerelease: true`, so any 10.0.x SDK works).
- Visual Studio on Windows only for `Bat.SqlClrAssembly.sqlproj` (SSDT).

## Build
```bash
dotnet build Bat.NoSql.slnf -c Release -p:GeneratePackageOnBuild=false
```
`Bat.NoSql.slnf` = every project in `Bat.sln` except the SQL CLR project. Building `Bat.sln` with the dotnet CLI fails
on that project (`MSB4278 ... SSDT ... targets`) — expected. If you add a project to `Bat.sln`, add it to `Bat.NoSql.slnf` too.

Single project: `dotnet build src/Core/Bat.Core/Bat.Core.csproj -c Release -p:GeneratePackageOnBuild=false`.

Expected result: 0 warnings, 0 errors. There are no security pins at the moment (10.0.14 removed them: the parent packages
now bring patched versions). If a new `NU1903` warning appears, pin the patched version in the "Security pins" section of
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
dotnet test Bat.NoSql.slnf            # or: dotnet test tests/Bat.Regression.Tests
```
`tests/Bat.Regression.Tests` (xUnit) needs no network, database, Redis or RabbitMQ:
- `CoreTests.cs` — JSON conventions, shared options, known ciphertexts (data compatibility!), validators, Persian date/time, misc fixes.
- `DataAccessTests.cs` — `BatDbContext` SaveChanges pipeline (EF InMemory), changed-entity helpers, dynamic `OrderBy`, factories, Dapper TVP, memory cache.
- `HttpTests.cs` — `HttpRequestTools` against an in-process Kestrel server (headers, cookies, timeouts, PUT form), `ClientInfo` probe user agents.
- `ClientInfoTests.cs` — User-Agent detection (~80 real user agents: OS, browser, engine, device type, manufacturer/model), client hints, hostile input.

`HttpTests.cs` calls `http://localhost`: if the machine has `HTTP_PROXY` / `HTTPS_PROXY` set, add `localhost,127.0.0.1`
to `NO_PROXY`, otherwise those tests fail with empty responses (the requests go to the proxy).

Not covered by automated tests (need infrastructure): Redis provider/lock, RabbitMQ, SQL Server specifics, Swagger filters, tag helpers.
For 10.0.1 and 10.0.2 the Redis provider + distributed lock were verified manually against a local `redis-server`, and the RabbitMQ
producer/consumer against a local `rabbitmq-server` (200 concurrent publishes over one connection). Do the same when you
change those parts and describe what you verified.

`src/Test/Bat.Test` is **not** a test project; it is a published helper library for consumers' unit tests.

## Versioning
- All packages share one version: `<Version>` in `src/Directory.Build.props` (currently `10.0.14`).
- Major = .NET major (10). Patch for fixes/perf, minor for new APIs.
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
