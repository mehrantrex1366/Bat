# Bat documentation (for agents and contributors)

Start with [../AGENTS.md](../AGENTS.md). These pages are written so you can work on one package without reading the
rest of the solution.

| Page | Use it for |
|---|---|
| [architecture.md](architecture.md) | Package graph, third-party dependencies, cross-cutting design (DI markers, entity markers, JSON, Persian specifics, hosting) |
| [conventions.md](conventions.md) | Compatibility rules, performance rules (do/don't table), safety rules, style |
| [build-test-release.md](build-test-release.md) | Build, test, pack, versioning |
| [known-issues.md](known-issues.md) | Known bugs, security debt and design issues intentionally not changed yet |
| [../CHANGELOG.md](../CHANGELOG.md) | What changed in each version and why |
| [../Specs/](../Specs/README.md) | Specs of structural changes (e.g. Central Package Management): requirements, design, verification, runbooks |

## Package reference
| Package | Page |
|---|---|
| Bat.Core | [reference/Bat.Core.md](reference/Bat.Core.md) |
| Bat.Tools | [reference/Bat.Tools.md](reference/Bat.Tools.md) |
| Bat.Cache | [reference/Bat.Cache.md](reference/Bat.Cache.md) |
| Bat.Cache.Redis | [reference/Bat.Cache.Redis.md](reference/Bat.Cache.Redis.md) |
| Bat.Http | [reference/Bat.Http.md](reference/Bat.Http.md) |
| Bat.AspNetCore | [reference/Bat.AspNetCore.md](reference/Bat.AspNetCore.md) |
| Bat.Di | [reference/Bat.Di.md](reference/Bat.Di.md) |
| Bat.EntityFrameworkCore | [reference/Bat.EntityFrameworkCore.md](reference/Bat.EntityFrameworkCore.md) |
| Bat.EntityFrameworkCore.Tools | [reference/Bat.EntityFrameworkCore.Tools.md](reference/Bat.EntityFrameworkCore.Tools.md) |
| Bat.Dapper | [reference/Bat.Dapper.md](reference/Bat.Dapper.md) |
| Bat.Queue | [reference/Bat.Queue.md](reference/Bat.Queue.md) |
| Bat.Test (helpers) | [reference/Bat.Test.md](reference/Bat.Test.md) |
| Bat.SqlClrAssembly | [reference/Bat.SqlClrAssembly.md](reference/Bat.SqlClrAssembly.md) |

The per-package `src/**/readme.md` files are the NuGet package readmes (end-user oriented, partly Persian); these docs are
the maintainer view and take precedence when they disagree.

## Keeping docs current
When a change alters behavior, public surface, defaults or dependencies of a package, update its page, `CHANGELOG.md` and,
if relevant, `known-issues.md` in the same change.
