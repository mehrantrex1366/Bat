# Tasks

- [x] Create branch `feature/central-package-management` from `Net10`.
- [x] Baseline: build, test and `dotnet pack Bat.NoSql.slnf`; save every `.nuspec` and package file list.
- [x] Add `Directory.Packages.props` with `ManagePackageVersionsCentrally=true` and all 36 package versions, grouped
      (Microsoft / third-party / security pins / Bat.Test / test tooling).
- [x] Remove `Version=` from every `<PackageReference>` (12 projects; Bat.Core has no package references).
- [x] Move `TargetFramework` and `ImplicitUsings` to the root `Directory.Build.props` (csproj-only condition).
- [x] Add `src/Directory.Build.props` (imports root; shared `Version`, `Authors`, `Company`, `Copyright`, `NeutralLanguage`,
      `RepositoryType`, `RepositoryUrl`, `IsPackable`).
- [x] Add `src/Directory.Build.targets` (icon/readme by file convention).
- [x] Remove the moved properties, empty `PackageIconUrl` / `PackageLicenseExpression` / `Company`, default
      `PackageRequireLicenseAcceptance=false`, and the `<None Update="Bat.png|readme.md">` blocks from each csproj.
- [x] Keep per-package texts byte-identical (restored two blank lines in Bat.Dapper that a cleanup pass had removed).
- [x] Bat.AspNetCore: exclude `appsettings.json` from the package.
- [x] Re-run build, tests, pack; compare against the baseline (see verification.md).
- [x] Update `AGENTS.md` (rules 7 and 9, "Where to look"), `docs/build-test-release.md`, `docs/known-issues.md`, `CHANGELOG.md`.
- [ ] Owner: open `Bat.sln` in Visual Studio and build `Bat.SqlClrAssembly` (SSDT can't be built with the dotnet CLI).
- [x] Set the shared version to `10.0.12` (`src/Directory.Build.props`) and the CHANGELOG section to 10.0.12.
- [x] Set the repository address to `https://github.com/mehrantrex1366/Bat` (`RepositoryUrl`, `PackageProjectUrl`, root README).
- [x] Re-pack and compare: only version and repository URL differ from the CPM build.
- [ ] Owner: merge into `Net10`.
