# Mutation Testing Policy

Mutation tests measure how well the test suite would catch real regressions: Stryker mutates production code (flip operators, invert conditions, swap return values) and re-runs the suite per mutation. A "killed" mutation = a test caught it. The mutation score is the fraction killed.

A failing mutation gate is a louder signal than a coverage gate — coverage just measures lines executed, not whether the tests would catch a bug in those lines.

## Per-layer targets

| Layer | High | Low | Break (CI fail) |
|---|---|---|---|
| Domain | 85 | 75 | **75** |
| Shared (libraries) | 85 | 75 | **75** |
| Application | 75 | 60 | **60** |
| Infrastructure | 60 | 45 | 0 (advisory — replays + EF + Wolverine paths are hard to mutate cleanly) |
| Api | 65 | 50 | 5–15 (advisory — minimal logic, most behaviour delegated to handlers) |

Targets match the issue #467 proposal. The Domain and Application targets are blocking; Infrastructure/Api stay advisory until we have a deterministic mutation strategy for the bus/handler dispatch layers.

## Current state vs. targets

Configs in `tests/Yumney.*/stryker-config.json`. Snapshot:

| Project | Current `break` | Target | Gap |
|---|---:|---:|---|
| Yumney.Recipes.Domain | 75 | 75 | ✓ |
| Yumney.Shopping.Domain | 75 | 75 | ✓ |
| Yumney.Users.Domain | 75 | 75 | ✓ |
| Yumney.MealPlan.Domain | 65 | 75 | **−10** |
| Yumney.Shared | 75 | 75 | ✓ |
| Yumney.Shared.Events | 75 | 75 | ✓ |
| Yumney.Shared.Events.Contracts | 75 | 75 | ✓ |
| Yumney.Shared.Guards | 60 | 75 | **−15** |
| Yumney.Recipes.Application | 45 | 60 | **−15** |
| Yumney.MealPlan.Application | 55 | 60 | **−5** |
| Yumney.Shopping.Application | 40 | 60 | **−20** |
| Yumney.Users.Application | 45 | 60 | **−15** |

Six projects are below target. The break thresholds were calibrated to the actual scores at registration time so passing builds didn't suddenly start failing. Closing the gap is per-project mutation-test work: read the Stryker HTML report (uploaded as `stryker-report-{project}` artifact on every PR run), find the surviving mutants, add tests that kill them, raise the `break` value.

## How to enforce

The mutation-testing workflow (`.github/workflows/mutation-testing.yml`) already exits non-zero when a project's mutation score drops below its `break` threshold. To make that a **blocking gate** on PRs targeting `develop`:

1. GitHub → repo **Settings** → **Branches** → **Branch protection rules** → edit the rule for `develop`.
2. Under **Status checks**, add every `Backend: <Project>` job name from the mutation-testing matrix as required.
3. Optionally check **Require branches to be up to date before merging** so rebased branches re-run mutation testing.

The workflow already publishes per-project results; once they're in the required-checks list, a regression below break blocks the merge.

## Raising a break threshold

1. Pull the latest mutation report for the project (Stryker HTML, downloadable from the GitHub Actions run artifacts).
2. Identify surviving mutants — they're highlighted in red in the HTML.
3. Add tests that kill them. Common patterns:
   - **Boundary mutants** (`>` → `>=`): add a test at the boundary value.
   - **Conditional mutants** (negated `if`): assert both branches.
   - **Statement removal**: assert side effect (event emitted, repository called).
4. Re-run locally: `cd tests/Yumney.Foo.Tests && dotnet stryker`.
5. Once the project's actual score exceeds the new target, raise `break` in `stryker-config.json`.

Don't raise `break` aggressively to "force" coverage work — a build that fails on every commit isn't useful. Raise in 5–10 point increments.

## What this policy does NOT enforce

- **Mutation testing on the frontend.** StrykerJS + Vitest has a known destructure bug that we hit; the workflow runs the JS suite advisory-only (see comment in the workflow). Re-enable when [stryker-mutator/stryker-js#X] lands.
- **Per-file targets.** Stryker supports `mutate` patterns to scope coverage to specific files; we use whole-project breaks today. Per-file granularity is a follow-up if specific hot paths need stronger guarantees.
