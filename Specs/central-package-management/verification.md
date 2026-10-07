# Verification

Environment: Windows 11, .NET SDK 10.0.401, branch `feature/central-package-management` (base `a63233e`), 2026-10-07.

## Commands

```bash
dotnet build Bat.NoSql.slnf -c Release -p:GeneratePackageOnBuild=false
dotnet test  Bat.NoSql.slnf -c Release --no-build
dotnet pack  Bat.NoSql.slnf -c Release --no-build -o <dir>
# per package: unzip -p X.nupkg '*.nuspec' ; unzip -l X.nupkg   → compared before vs. after
grep -rn 'Version="' --include=*.csproj src tests
```

## Results

| Check | Before (Net10 @ a63233e) | After |
|---|---|---|
| Build | 0 warnings, 0 errors | 0 warnings, 0 errors |
| Tests | 29/29 passed | 29/29 passed |
| `Version=` on PackageReference | 36 | 0 |
| Packages produced by `dotnet pack Bat.NoSql.slnf` | 10 (Bat.Tools **failed** with `NU5046`, Bat.AspNetCore **skipped**) | 12, each with `.snupkg` |
| Pack warnings | `NU5104` (Bat.Di), `NU5046` error (Bat.Tools), "packaging disabled" (Bat.AspNetCore, tests) | `NU5104` (Bat.Di), "packaging disabled" (tests only — expected) |

## Package comparison

| Package | Result |
|---|---|
| Bat.Core, Bat.Cache, Bat.Cache.Redis, Bat.Dapper, Bat.Di, Bat.EntityFrameworkCore, Bat.EntityFrameworkCore.Tools, Bat.Http, Bat.Queue | nuspec and file list **identical** to baseline |
| Bat.AspNetCore | new: `10.0.2`, shared metadata, icon + readme, dependencies Bat.Core / JwtBearer 10.0.12 / Microsoft.OpenApi 2.12.2 / Swashbuckle 10.2.3, `frameworkReference Microsoft.AspNetCore.App`; **no** `appsettings.json` |
| Bat.Tools | new (previously failed): `10.0.2`, icon + readme, dependencies Bat.Core / EPPlus 8.7.1 / EPPlus.DataExtractor 2.2.0 / System.Security.Cryptography.Xml 10.0.12 |
| Bat.Test | `1.0.0` → `10.0.2`; authors `Bat.Test` → `Mehran Norouzi`; adds copyright, `readme`; repository URL now the configured `RepositoryUrl`; dependencies unchanged |

Dependency versions in every package are the same as before (R3).

## Not verified here

- `Bat.SqlClrAssembly.sqlproj`: SSDT builds only in Visual Studio. Its imported property groups are guarded by
  `MSBuildProjectExtension == '.csproj'` and it has no package references, so no effect is expected — confirm by building it in VS.
- NuGet Package Explorer "Health" (Source Link / deterministic / compiler flags) needs `-p:ContinuousIntegrationBuild=true` and a
  pushed commit; not re-checked in this change.

## Version 10.0.12 and repository address

The package version was then set to `10.0.12` and the repository address to `https://github.com/mehrantrex1366/Bat`
(it was `mehrannoruzi/Bat`, which did not match the git remote that Source Link uses). Re-run on the same commit:

| Check | Result |
|---|---|
| Build | 0 warnings, 0 errors |
| Tests | 29/29 passed |
| Pack | 12 `.nupkg` + 12 `.snupkg`, all `10.0.12`; only warning `NU5104` (Bat.Di) |
| All 12 nuspecs vs. the CPM build above | identical except `<version>`, `Bat.*` dependency versions (`10.0.12`) and the repository / project URL |
| All 12 file lists vs. the CPM build above | identical |
| Bat.EntityFrameworkCore | `<projectUrl>` and `<repository url>` = `https://github.com/mehrantrex1366/Bat` |

The nuspec repository URL and the Source Link URLs (from the git remote) now point to the same repository.
