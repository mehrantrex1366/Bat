# Specs

Specifications for changes to the Bat repository: what we wanted, how we did it, how it was verified, and the rules that
follow from it. `docs/` describes the code as it is; `Specs/` explains why a structural change was made and how to keep it working.

Each spec lives in its own folder, named after its branch topic (`feature/<topic>` → `Specs/<topic>/`):

| File | Contents |
|---|---|
| `README.md` | Summary, status, links to the other files |
| `requirements.md` | Goals, non-goals, requirements and acceptance criteria |
| `design.md` | The chosen design, file layout, alternatives considered |
| `tasks.md` | Implementation checklist |
| `verification.md` | What was run and the results |
| `runbook.md` | Day-to-day procedures that follow from the change (optional) |

## Index

| Spec | Status | Branch |
|---|---|---|
| [central-package-management](central-package-management/README.md) | Implemented | `feature/central-package-management` |
