# ADR 0001 — Persistence paradigm per aggregate

- Status: Accepted
- Date: 2026-04-20
- Last revised: 2026-05-08
- Context: Architecture review US-000

## Context

Yumney currently uses two persistence paradigms in parallel:

- **State-based (EF Core)** — the write model is the current state of the
  aggregate. `Recipe`, `RecipeFavorite`, `AppUserProfile`, `StaplesList`,
  and similar aggregates store a row per entity; EF Core tracks changes
  and emits UPDATE statements. Domain events are raised by aggregate
  methods and dispatched via `DomainEventDispatchInterceptor` after
  `SaveChanges`. Base class: `AggregateRoot<TId>`.

- **Event-sourced** — the aggregate is rebuilt by replaying a stream of
  domain events. As of the last revision, three aggregates are
  event-sourced: `ShoppingLedger`, `ShoppingList` (Shopping module),
  and `WeeklyPlan` (MealPlan module). Each has its own
  `EventStoreBase`-derived store; the corresponding projection handlers
  (`ShoppingLedgerProjectionHandler`, `ShoppingListProjection`,
  `MealPlanProjectionHandler`) maintain the read model via
  `IModuleEventHandler` subscriptions. Base class:
  `EventSourcedAggregate<TId>`, versioned with `AggregateVersion`.

The architecture review flagged the inconsistency as a risk: newcomers
can't tell from a module what paradigm applies, the two models have
different failure modes (state-based tolerates schema drift; event-
sourced tolerates replay), and tooling (projections, snapshots, migrations)
only exists for one of them.

## Decision

Keep both paradigms. Do not migrate existing aggregates. Apply the
following rules when introducing a new aggregate or revisiting an
existing one:

**Use event-sourcing when all of the following hold:**

- The aggregate's business value depends on a verifiable timeline
  ("who bought what, when, from where") — i.e. the history is the
  product, not a byproduct.
- External consumers subscribe to the aggregate's events for their
  own read models or analytics.
- Retroactive replay is part of the operating model (rebuilding a
  stale projection, running a new report against old data).

**Use state-based persistence (the default) when any of the following hold:**

- The aggregate's value is captured adequately by its current state.
- Domain events exist only to notify within the bounded context.
- Schema evolution via EF migrations is cheaper than event-stream
  versioning for the team.

For modules that mix usage (e.g. Recipes mixes a state-based `Recipe`
with state-based `RecipeFavorite`, while Shopping has two event-sourced
aggregates — `ShoppingList` and `ShoppingLedger` — that share a single
event store database), keep the aggregates in separate folders under
`Domain/`, with a short README in the module describing why each
paradigm was chosen.

## Consequences

**Positive**

- Existing code stays: no migration risk, no cross-cutting refactor.
- New aggregates get an explicit, documented decision instead of
  following whichever example a developer saw first.
- The review's inconsistency finding is resolved by being explicit,
  not by picking one paradigm globally.

**Negative**

- Two base classes (`AggregateRoot<TId>`, `EventSourcedAggregate<TId>`)
  and two persistence patterns must remain documented and supported.
- Projection infrastructure (event store, read-model projector,
  integration event integration) is duplicated surface area — any
  bug fix or feature in one paradigm needs a conscious decision about
  the other.
- Tooling investments (snapshot support, CI query-count gates,
  integration-event inbox) must land in both paradigms where relevant.

## Follow-ups

- Add a short README next to `ShoppingLedger/` explaining why that
  aggregate is event-sourced, so the module-level rationale is
  discoverable.
- When a future aggregate is proposed, require the design notes to
  reference this ADR and state the decision explicitly.
- Revisit this ADR if the ratio of event-sourced aggregates crosses
  50% — at that point a third option (uniform event-sourcing with
  opt-in snapshots) becomes plausible.

## Revision history

- **2026-05-08** — Updated context to reflect that `ShoppingList`
  (migration `20260428212832_AddShoppingListEventStore`) and
  `WeeklyPlan` (migration `20260428145622_ReplaceWeeklyPlanWithEventSourcing`)
  were converted to event sourcing after this ADR was originally
  accepted. Raised the revisit threshold from 30% to 50% because the
  current ratio (3 of ~7 aggregates) already brushes the original
  trigger without indicating a need to re-decide.
