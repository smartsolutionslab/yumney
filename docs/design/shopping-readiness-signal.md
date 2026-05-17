# Design: Shopping read-side readiness signal

**Refs:** #606 (the flake mitigation #743 and the flake-hunt #746 ship the test-only mitigations; this doc covers the production-side structural fix.)

## Motivation

Integration tests that exercise `POST /shopping-lists/.../check` followed by `GET /shopping-lists/{id}` race the async projection handler. The current best mitigation is the `Eventually.AssertAsync` poll wrapper with a 90 s CI timeout — but every test still spends real time waiting and the poll itself burns HTTP round-trips. We can replace this with a per-aggregate readiness watermark that the read endpoint blocks on, eliminating client-side polling.

Out of scope: replacing eventual consistency with strong consistency. The projection stays asynchronous; what changes is that callers can wait for a specific point in event time without polling.

## Contract

### Write side

No change. Writes still return their existing status codes + bodies. Tests capture `var t0 = DateTimeOffset.UtcNow;` immediately before the write call.

### Read side

Endpoints that read the shopping-list projection accept an optional query param:

```
GET /api/v1/shopping-lists/{id}?waitForUpdatedAfter=2026-05-17T10:23:45.123Z
```

Semantics:
- When the param is **absent**: behaves exactly as today — returns the current read model state, no waiting.
- When **present**: server-side polls the read model until `summary.LastUpdated >= waitForUpdatedAfter`, then returns the row. If the watermark never crosses the threshold within a hard-coded 30 s cap, returns **504 Gateway Timeout** with an RFC 7807 problem body (no body change beyond status — the row data wouldn't be authoritative either way).

The 30 s cap is per-request server-side; it's independent of YARP / Polly timeouts and represents "the projection should be done by now or something is broken."

### Why `waitForUpdatedAfter` and not `waitForEventId`

`waitForEventId` would be perfectly deterministic but requires:
- Returning a stable event id from writes (currently 204 NoContent)
- Tracking per-event-id high-water-mark instead of timestamp

A timestamp watermark is "good enough" because `OccurredAt` is monotonically non-decreasing per aggregate (the event store assigns it on append, in append order). A test that captures `t0` before its own write and waits for `LastUpdated > t0` will always wait at least until its own event is projected — earlier unrelated events for the same aggregate cannot prematurely satisfy the wait if their `OccurredAt < t0`.

## Projection changes

### LastUpdated semantics today

The summary row's `LastUpdated` is updated on:
- `ShoppingListCreated` (sets to `Inner.CreatedAt`)
- `ListItemAdded` (sets to `DateTime.UtcNow`)
- `AllItemsChecked` / `AllItemsUnchecked` (via `TouchSummaryAsync` → `DateTime.UtcNow`)
- `RecipeReferenceCleared` (sets to `DateTime.UtcNow`)

NOT updated on:
- `ListItemChecked` / `ListItemUnchecked`
- `ListItemCategoryChanged`

### Required changes

1. **Use `OccurredAt`, not `DateTime.UtcNow`.** Plumb `OccurredAt` (UTC) through every `*ModuleEvent` wrapper. The domain event already carries this in event-sourced aggregates; the module event needs to expose it.
2. **Touch the summary's `LastUpdated` on every aggregate event.** Add `TouchSummaryAsync(listId, occurredAt)` calls to the two checked/unchecked handlers and the category handler. The summary becomes a monotonically non-decreasing per-list watermark.
3. **Use the event's `OccurredAt` consistently** in every handler that sets `LastUpdated` (today it's a mix of `Inner.CreatedAt` and `DateTime.UtcNow`).

### Backfill

Existing rows keep their current `LastUpdated` value. No migration needed — the column already exists. Tests that capture `t0` after this change will block on the new event's `OccurredAt`, which is always >= write time, so they're correct from the moment the change ships.

## Server-side polling shape

In the Shopping API endpoint handler:

```csharp
var deadline = DateTimeOffset.UtcNow + TimeSpan.FromSeconds(30);
while (DateTimeOffset.UtcNow < deadline)
{
    var summary = await repository.GetSummaryAsync(listId, ct);
    if (summary is null) return Results.NotFound();
    if (waitForUpdatedAfter is null || summary.LastUpdated >= waitForUpdatedAfter)
    {
        return Results.Ok(summary);
    }
    await Task.Delay(50, ct);
}
return Results.StatusCode(StatusCodes.Status504GatewayTimeout);
```

50 ms poll is server-side only; the test pays one HTTP round-trip total instead of N. The handler stays idempotent — no global state, no signalling primitives.

## Test migrations

Affected tests (the original #606 offenders + the high-flake-risk reads migrated under #746):

| Test | New shape |
|---|---|
| `CheckOffItemContractTests.CheckOff_IsCheckedTrue_…` | capture `t0` before PUT, GET with `?waitForUpdatedAfter={t0:O}` |
| `ShoppingListCreateFlowTests.CreateAndCheckOff_…` | same pattern |
| `GetMergedShoppingListContractTests.GetMerged_AfterAddManualItem_…` | same pattern (already polls under #746; tighten with explicit watermark) |
| `ExportShoppingListContractTests.Export_AfterAddManualItem_…` | same pattern |

The `Eventually.AssertAsync` wrapper stays for the cross-module and unrelated cases where the watermark doesn't fit.

## Rollout

1. **Land projection changes first** (touch summary on every event + use `OccurredAt`). No external behaviour change. Smoke test the existing `Eventually.AssertAsync` tests still pass.
2. **Add the endpoint query param** in a second commit/PR. Tests that don't use it stay green; the new code path is exercised only by callers passing the param.
3. **Migrate the flaky tests** in a third commit/PR. Verify reduced flake rate over a week of runs.

Three small PRs are safer than one big one — the projection semantic change is the riskiest piece and worth its own review window.

## Non-goals

- **Cross-aggregate watermarks.** Each list has its own `LastUpdated`; there's no "global" projection watermark. Tests that touch multiple lists in one operation pass `waitForUpdatedAfter` per list.
- **Stronger durability guarantees.** This is purely about test determinism. Production callers don't need the param.
- **Replacing `Eventually.AssertAsync`.** The wrapper still covers cross-module cases (e.g. RecipeDeleted → Shopping cascade) where the consumer-side watermark isn't accessible from the test.

## What changes in CLAUDE.md

Nothing. This is an internal projection-handler refinement; the public API gains an optional query param, which doesn't change any architectural rule.
