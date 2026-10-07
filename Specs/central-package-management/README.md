# Central Package Management (CPM) and shared build files

| | |
|---|---|
| Status | Implemented; package version **10.0.12**, not yet published (see `CHANGELOG.md`) |
| Branch | `feature/central-package-management` (from `Net10` @ `a63233e`) |
| Scope | Build files only: no C# source, public API or dependency version changed. Also: package version → `10.0.12`, repository → `mehrantrex1366/Bat` |

## Summary

Every NuGet package version is now declared once in [`Directory.Packages.props`](../../Directory.Packages.props), and settings that
were copied into each of the 13 `.csproj` files now live in shared MSBuild files:

| File | Applies to | Holds |
|---|---|---|
| `Directory.Packages.props` | all projects | `ManagePackageVersionsCentrally` + every `<PackageVersion>` |
| `Directory.Build.props` | all projects | `TargetFramework`, `ImplicitUsings` (csproj only), Source Link, `snupkg`, deterministic |
| `src/Directory.Build.props` | `src/**/*.csproj` | `Version`, authors, company, copyright, language, repository, `IsPackable` |
| `src/Directory.Build.targets` | `src/**/*.csproj` | `PackageIcon` / `PackageReadmeFile` and their pack items, when `Bat.png` / `readme.md` exist |

The csproj files keep only what is specific to them: Description, PackageTags, PackageReleaseNotes, Product,
`GeneratePackageOnBuild`, package/project references and resources.

As a side effect three existing packaging defects were fixed: Bat.AspNetCore was never packed, Bat.Tools failed to pack
(`NU5046`) and Bat.Test was packed as version `1.0.0`. The other nine packages are byte-for-byte identical in metadata and file list.

## Documents

- [requirements.md](requirements.md) — goals, non-goals, acceptance criteria
- [design.md](design.md) — file layout, evaluation order, decisions and rejected alternatives
- [tasks.md](tasks.md) — implementation checklist
- [verification.md](verification.md) — commands run and before/after package comparison
- [runbook.md](runbook.md) — how to upgrade a package, add a project, pin a vulnerable dependency, release

## Rules (short)

1. Never write `Version=` on a `<PackageReference>`. Add or change the `<PackageVersion>` in `Directory.Packages.props`.
2. Bump the shared package version only in `src/Directory.Build.props`.
3. Don't repeat shared metadata (authors, copyright, repository, icon, readme, target framework) in a csproj.
4. Security pins go in the "Security pins" group of `Directory.Packages.props` **and** keep an explicit `<PackageReference>`
   in the project that needs them (CPM transitive pinning is off on purpose; see design.md).
