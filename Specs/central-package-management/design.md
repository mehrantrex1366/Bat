# Design

## File layout

```
Bat/
├─ Directory.Packages.props      ← every NuGet version (CPM)
├─ Directory.Build.props         ← all projects: TFM, implicit usings, Source Link / symbols / deterministic
├─ src/
│  ├─ Directory.Build.props      ← imports ../Directory.Build.props, then shared package metadata
│  ├─ Directory.Build.targets    ← icon + readme for packages that have the files
│  └─ <Area>/<Package>/<Package>.csproj   ← description, tags, release notes, references only
└─ tests/
   └─ Bat.Regression.Tests/…csproj        ← gets root Directory.Build.props only (not packable)
```

### How MSBuild picks the files

- `Directory.Build.props` / `.targets`: MSBuild imports only the **nearest** one walking up from the project. That is why
  `src/Directory.Build.props` explicitly imports the root one with `GetPathOfFileAbove`. Projects under `tests/` get only the root file,
  so they don't inherit package metadata or `IsPackable=true`.
- `Directory.Packages.props`: imported by the NuGet SDK targets from the nearest folder; there is one, at the root.
- `.props` files are evaluated **before** the project body, `.targets` **after** it and after the SDK's default items. So:
  - properties a project may override (Version, metadata) are in `.props`; a csproj value wins;
  - the icon/readme logic is in `.targets` because `<None Update="Bat.png">` only works after the SDK has globbed `None` items,
    and because it checks whether the project already set `PackageIcon`/`PackageReadmeFile`.

### Guarding non-SDK projects

`Bat.SqlClrAssembly.sqlproj` (SSDT, `TargetFrameworkVersion v4.8`) is under `src/` and also imports `Directory.Build.props`.
Every property group that would affect it is conditioned on `'$(MSBuildProjectExtension)' == '.csproj'`, so it sees only the
Source Link/symbol properties (which it ignores). It has no `PackageReference`, so CPM does not affect it.

## Decisions

### D1 — CPM without transitive pinning
`CentralPackageTransitivePinningEnabled` is **off**. With it on, every `PackageVersion` would also pin transitive dependencies,
and NuGet adds such pinned packages as direct dependencies in the produced `.nuspec`. That would change the dependency list of
published packages (violates R6) and pin, for example, `Microsoft.EntityFrameworkCore` in packages that only get it transitively.
Instead the two security pins (`Microsoft.OpenApi`, `System.Security.Cryptography.Xml`) remain explicit `<PackageReference>`s in
the project that needs them, with their versions in the "Security pins" group.

### D2 — One version for all packages, in `src/Directory.Build.props`
AGENTS.md rule 7 already required all packages to share one version and be bumped together. Setting it once makes that rule
enforce itself. A csproj can still override it, but must not (documented in AGENTS.md rule 9).

### D3 — Icon and readme by convention
`src/Directory.Build.targets` sets `PackageIcon=Bat.png` / `PackageReadmeFile=readme.md` only if the file exists next to the
csproj and the project did not set its own, and adds the matching `Pack="true"` item. This removed 21 copies of the same
`<None Update>` block and fixed Bat.Tools (`NU5046`). Bat.Queue (no readme) and Bat.Test (no icon) work without special cases.

### D4 — Bat.AspNetCore stays on `Microsoft.NET.Sdk.Web`
Switching it to `Microsoft.NET.Sdk` + `<FrameworkReference Include="Microsoft.AspNetCore.App" />` would be cleaner but changes
`OutputType` (Exe → Library) and would make the public `Bat.AspNetCore.Mvc.Program` pointless; that is an API decision (rule 1).
Kept as is: `IsPackable=true` comes from `src/Directory.Build.props`; `appsettings.json` gets `Pack="false"` in the csproj.

### D5 — Untouched on purpose
Per-package Description / PackageTags / PackageReleaseNotes (texts differ, including blank lines and phone-number format),
`GeneratePackageOnBuild` (differs per project), `Product`, `UserSecretsId`, resources, `Scc*` properties.

## Alternatives considered

| Alternative | Why not |
|---|---|
| Keep versions in csproj, add a check script | Doesn't remove duplication; CPM is the supported NuGet mechanism. |
| One root `Directory.Build.props` with path conditions for `src/` | Harder to read; nested file + explicit import is the documented pattern. |
| `VersionOverride` for the security pins | Defeats CPM (version back in a csproj). Not needed: pins are ordinary central versions. |
| `Directory.Build.rsp` / `.slnx` migration | Unrelated to the goal. |
