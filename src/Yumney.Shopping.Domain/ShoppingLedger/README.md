# ShoppingLedger — event-sourced aggregate

`ShoppingLedger` is persisted as an event stream, not a state row.
Commands produce `ShoppingItemAdded`, `ShoppingItemBought`,
`ShoppingItemConsumed`, etc.; the aggregate is rebuilt by replaying
those events via `EventSourcedAggregate<ShoppingLedgerIdentifier>`.
`EfCoreShoppingEventStore` handles persistence;
`ShoppingListProjectionHandler` maintains the read model.

## Why event-sourcing here

The ledger answers "what did the user shop for, when, from which
source?" over time — the history _is_ the business value. Removing
an item is a past event, not a state mutation. Reporting, replay
for read-model drift, and integration-event publication to other
modules all require a durable stream.

By contrast, `ShoppingList` (the curated list the user edits) only
needs its current state, so it stays state-based under
`ShoppingList/`.

See [ADR 0001](../../../docs/adr/0001-persistence-paradigm-per-aggregate.md)
for the project-wide rule.
