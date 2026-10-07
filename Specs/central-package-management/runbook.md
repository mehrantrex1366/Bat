# Runbook

## Upgrade a package

1. Change its `<PackageVersion>` in `Directory.Packages.props`. Keep the `10.0.x` Microsoft family on the same servicing release.
2. If it is the parent of a security pin (Swashbuckle → Microsoft.OpenApi, EPPlus → System.Security.Cryptography.Xml),
   check whether the pin is still needed; remove both the `PackageVersion` and the project's `PackageReference` if not.
3. Build (0 warnings), test, and add a row to the dependency table in `CHANGELOG.md`.

List outdated packages: `dotnet list Bat.NoSql.slnf package --outdated` (works with CPM).

## Add a package to a project

```xml
<!-- Directory.Packages.props (if the package is new to the repository) -->
<PackageVersion Include="Some.Package" Version="1.2.3" />

<!-- the csproj: no version -->
<PackageReference Include="Some.Package" />
```

A `<PackageReference>` with `Version=` fails the restore with `NU1008`. Without a matching `<PackageVersion>` it fails with `NU1010`.
Visual Studio's NuGet UI and `dotnet add package` both write to `Directory.Packages.props` automatically.

## Pin a vulnerable transitive package (`NU1903`)

1. Add `<PackageVersion>` with the patched version to the "Security pins" group, with a comment naming the parent package.
2. Add `<PackageReference Include="…" />` with a `<!-- Pinned: … -->` comment to the project that pulls it in.
3. Don't turn on `CentralPackageTransitivePinningEnabled`; it would change the dependency list of the published packages (design.md D1).

## Add a new package project

1. Create it under `src/<Area>/<Name>/`. Use `Microsoft.NET.Sdk` (or `Sdk.Web`; `IsPackable` is set to `true` for you).
2. Put `Bat.png` and a `readme.md` next to the csproj. Icon and readme are then packed automatically.
3. In the csproj set only: `Description`, `PackageTags`, `PackageReleaseNotes`, `GeneratePackageOnBuild` if needed, references.
   Don't set `TargetFramework`, `Version`, authors, copyright, repository, icon or readme.
4. Add it to `Bat.sln` **and** `Bat.NoSql.slnf`, and add `docs/reference/<Name>.md` + a row in `AGENTS.md`.

Projects under `tests/` get only the root `Directory.Build.props` (no package metadata); set `IsPackable=false` there.

## Release

1. Bump `<Version>` in `src/Directory.Build.props` (all packages move together).
2. Move the "Unreleased" section of `CHANGELOG.md` under the new version.
3. Commit and push, then pack from that commit:
   ```bash
   dotnet pack Bat.NoSql.slnf -c Release -p:ContinuousIntegrationBuild=true -o ./artifacts
   ```
4. Expect 12 `.nupkg` + 12 `.snupkg`. Publishing is done by the owner (`dotnet nuget push` sends the `.snupkg` along).

## Check what a project really gets

```bash
dotnet msbuild src/Core/Bat.Tools/Bat.Tools.csproj -getProperty:Version -getProperty:PackageIcon -getProperty:IsPackable
dotnet msbuild src/Core/Bat.Tools/Bat.Tools.csproj -preprocess:out.xml   # fully expanded project, shows every import
```
