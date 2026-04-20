# ShoppingListProjectionHandler profiling

The projection handler runs once per integration event and is the hottest
path in the shopping module's read side. This note records the expected
query budget, the current hotspots, and how to measure them.

## Expected query budget per event

| Event | SELECT | INSERT / UPDATE | DELETE | Total commands |
|---|---|---|---|---|
| `ShoppingItemAdded` (existing item) | 1 | 1 (UPDATE) | 0 | 2 |
| `ShoppingItemAdded` (new item) | 1 | 1 (INSERT) | 0 | 2 |
| `ShoppingItemBought` | 1 | 1 | 0 | 2 |
| `ShoppingItemConsumed` | 1 | 1 | 0 | 2 |
| `ShoppingItemRemoved` (partial) | 1 | 1 | 0 | 2 |
| `ShoppingItemRemoved` (last unit) | 1 | 0 | 1 | 2 |
| `ShoppingItemQuantityAdjusted` | 1 | 1 | 0 | 2 |

Any handler that exceeds 2 commands is a regression — most likely an
accidental `.Include(...)` added on the `FindAsync` query, or a batch
consumer loop that hits this handler once per item without batching.

## Snapshotting

`Yumney.Shared.Persistence.QueryCountingInterceptor` (added in roadmap
#14) increments an `IQueryCounter` for every command executed. In
integration tests, resolve the counter from the DI container before
each test, snapshot `counter.Count`, publish the event through the
bus (or call `ShoppingListProjectionHandler.HandleAsync` directly),
and assert the delta equals the row in the table above:

```csharp
var counter = scope.ServiceProvider.GetRequiredService<IQueryCounter>();
counter.Reset();

await handler.HandleAsync(itemAddedEvent, CancellationToken.None);

counter.Count.Should().Be(2);
```

To activate the interceptor in a test `WebApplicationFactory`, add
`services.AddQueryCounting()` and resolve it in the `UseNpgsql`
callback:

```csharp
options
    .UseNpgsql(connection, ...)
    .AddInterceptors(sp.GetRequiredService<QueryCountingInterceptor>());
```

## Current hotspots (sorted by expected payoff)

1. **`SourcesJson` full deserialize → append → serialize per add.**
   Every `ShoppingItemAdded` on an existing row rewrites the entire
   JSON blob. For power-users with many imports, the blob grows
   monotonically and the write cost grows linearly. Fix: normalize
   sources into a `ShoppingListReadItemSource` child table with FK
   `ShoppingListReadItemId`. Appending becomes a single INSERT.
2. **`EF.Functions.ILike` on `ItemName`.** PostgreSQL can use the
   `(OwnerId, ItemName, Unit)` B-tree for `=` but not reliably for
   ILIKE even without wildcards (depends on collation). Fix: add a
   generated column `ItemNameNormalized = lower(ItemName)` with an
   index on `(OwnerId, ItemNameNormalized, Unit)`, and normalize at
   write time.
3. **Per-event `SaveChangesAsync`.** When Wolverine delivers a
   batch, each event pays the full round-trip. Lower-payoff — batch
   receive is rare — but worth measuring before normalizing sources.

Each of these lands as a separate follow-up under its own story;
none are blockers for the current read-model traffic.
