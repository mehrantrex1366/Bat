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

Expected result: 0 warnings, 0 errors. Vulnerable transitive packages are pinned explicitly (see csproj comments);
if a new `NU1903` warning appears, pin the patched version the same way.

## Test
```bash
dotnet test Bat.NoSql.slnf            # or: dotnet test tests/Bat.Regression.Tests
```
`tests/Bat.Regression.Tests` (xUnit) needs no network, database, Redis or RabbitMQ:
- `CoreTests.cs` — JSON conventions, shared options, known ciphertexts (data compatibility!), validators, Persian date/time, misc fixes.
- `DataAccessTests.cs` — `BatDbContext` SaveChanges pipeline (EF InMemory), changed-entity helpers, dynamic `OrderBy`, factories, Dapper TVP, memory cache.
- `HttpTests.cs` — `HttpRequestTools` against an in-process Kestrel server (headers, cookies, timeouts, PUT form), `ClientInfo` parsing.

Not covered by automated tests (need infrastructure): Redis provider/lock, RabbitMQ, SQL Server specifics, Swagger filters, tag helpers.
For 10.0.1 the Redis provider + distributed lock were verified manually against a local `redis-server`, and the RabbitMQ
producer/consumer against a local `rabbitmq-server` (200 concurrent publishes over one connection). Do the same when you
change those parts and describe what you verified.

`src/Test/Bat.Test` is **not** a test project; it is a published helper library for consumers' unit tests.

## Versioning
- All packages share one version (`<Version>` in each `src/**/*.csproj`, currently `10.0.1`).
- Major = .NET major (10). Patch for fixes/perf, minor for new APIs.
- Any change → bump **all** packages + entry in `CHANGELOG.md`.

## Pack / publish
```bash
dotnet pack Bat.NoSql.slnf -c Release -o ./artifacts
```
Packages include `Bat.png` and the package `readme.md` (`PackageReadmeFile`). `Bat.Queue` has no readme.
Publishing (NuGet feed and API key) is done by the owner; agents must not publish packages.

## After releasing
Consumers must update **all** Bat packages to the same version (e.g. a service that uses Bat.Cache.Redis and Bat.Http
must not mix 10.0.0 and 10.0.1), otherwise an older Bat.Core may be resolved.
