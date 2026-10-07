# Requirements

## Problem

- Package versions were spread over 13 csproj files. The same package (e.g. the `10.0.x` Microsoft.Extensions / EF Core
  family) had to be edited in several places on every update, and nothing prevented two projects drifting apart.
- The shared package version (`<Version>10.0.2</Version>`) and the same metadata (authors, company, copyright, repository,
  icon, readme) were repeated in every csproj with small, accidental differences:
  - `RepositoryType` was `GitHub` or `Microsoft DevOps Server` instead of `git`; Bat.Queue had no `RepositoryUrl`;
  - Bat.Dapper had an empty `Company`; Bat.Tools had `PackageIcon` but never packed `Bat.png` → `dotnet pack` failed with `NU5046`;
  - Bat.Test had no `Version`, so it was packed as `1.0.0` with authors `Bat.Test`;
  - Bat.AspNetCore (`Microsoft.NET.Sdk.Web`) was silently skipped by `dotnet pack` because Sdk.Web defaults to `IsPackable=false`.

## Goals

| # | Requirement |
|---|---|
| R1 | Enable NuGet Central Package Management: every package version is declared exactly once, in `Directory.Packages.props`. |
| R2 | No `<PackageReference>` in the repository carries a `Version` (or `VersionOverride`). |
| R3 | Resolved dependency versions do not change (this is a refactor, not an upgrade). |
| R4 | The shared package version is set in exactly one place. |
| R5 | Shared metadata and build settings are defined once and inherited; each csproj keeps only project-specific values. |
| R6 | Published package contents do not change, except to fix the defects listed above. |
| R7 | The SSDT project `Bat.SqlClrAssembly.sqlproj` (.NET Framework 4.8, Visual Studio only) is not affected. |
| R8 | Build stays at 0 warnings / 0 errors (including `NU1903`) and all regression tests pass. |
| R9 | Rules are documented for maintainers and AI agents (AGENTS.md, docs/, this spec). |

## Non-goals

- Upgrading any package (done separately, see 10.0.2).
- Changing C# code or public API. `Bat.AspNetCore.Mvc.Program` stays even though it is only there because of Sdk.Web.
- Harmonizing per-package texts (Description, ReleaseNotes) — they differ on purpose or are owner-authored.
- Removing the legacy TFVC `Scc*` = `SAK` properties (harmless; removal would only touch VS source-control bindings).
- Fixing `Bat.Cache.Redis` shipping `appsettings.json` as content (consumer-visible; recorded in `docs/known-issues.md`).
- Changing dependency versions. (The package version itself was set to `10.0.12` and the repository address to
  `https://github.com/mehrantrex1366/Bat` at the owner's request, in the same change.)

## Acceptance criteria

- [x] `grep -rn 'Version="' --include=*.csproj src tests` returns nothing.
- [x] `dotnet build Bat.NoSql.slnf -c Release -p:GeneratePackageOnBuild=false` → 0 warnings, 0 errors.
- [x] `dotnet test Bat.NoSql.slnf` → all tests pass (29/29).
- [x] `dotnet pack Bat.NoSql.slnf` produces 12 packages, each with a `.snupkg`; the only warning is the known `NU5104` on Bat.Di.
- [x] For the 9 packages that packed before: nuspec and file list identical to the baseline.
- [x] Bat.AspNetCore, Bat.Tools, Bat.Test packages: shared version (`10.0.12`), shared metadata, no `appsettings.json` in Bat.AspNetCore.
