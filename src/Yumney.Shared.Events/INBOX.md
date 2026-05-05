# Integration-event inbox

Every `IIntegrationEventHandler<T>` (and `IModuleEventHandler<T>`)
invocation goes through `IInboxStore.TryProcessAsync(messageId,
consumerName, handler)`. The store decides whether to invoke the
handler and shares a single transactional fate with the handler's
writes — the inbox `(messageId, consumerName)` row only commits if the
handler succeeds, and a thrown handler rolls the row back so a
redelivery can retry the work cleanly.

The contract is **delegate-shaped** rather than scope-returning. EF
Core's retrying execution strategies (Npgsql's `EnableRetryOnFailure`)
require the whole begin–work–commit sequence to live inside an
`IExecutionStrategy.ExecuteAsync(...)` lambda owned by the strategy;
returning a transaction handle to the caller throws
`InvalidOperationException: The configured execution strategy ...
does not support user-initiated transactions`. Threading the handler
through a delegate keeps the transaction inside the strategy boundary
and lets the strategy retry transient failures end-to-end.

## Outcomes

`TryProcessAsync` returns one of:

- `Processed` — the handler ran, the inbox row + handler writes
  committed atomically.
- `AlreadyProcessed` — the pre-check found an existing row; the
  handler was not invoked.
- `DuplicateRace` — a concurrent peer committed the same
  `(messageId, consumerName)` pair between pre-check and commit; the
  unique constraint fired and the entire local transaction
  (handler writes included) rolled back. The consumer logs and skips.

If the handler itself throws, the exception propagates out of
`TryProcessAsync`. The caller (the Wolverine event consumer) logs and
rethrows so the transport can retry the message; the retry will see no
inbox row and run the handler again.

## Default: `NoOpInboxStore`

The Wolverine bus registers `NoOpInboxStore` by default. It just
invokes the handler and reports `Processed`, matching pre-inbox
behaviour. No schema change is required to adopt the abstraction;
modules opt in to real deduplication on their own timeline.

## Activating the EF Core store per module

1. Add the `InboxMessage` DbSet + configuration to the module's write
   context:
   ```csharp
   public DbSet<InboxMessage> InboxMessages => Set<InboxMessage>();

   protected override void OnModelCreating(ModelBuilder modelBuilder)
   {
       modelBuilder.ApplyConfiguration(new InboxMessageConfiguration());
       modelBuilder.ApplyConfigurationsFromAssembly(typeof(MyDbContext).Assembly);
   }
   ```
2. Generate the migration:
   ```bash
   dotnet ef migrations add AddInboxMessages --project src/Yumney.<Module>.Infrastructure
   ```
3. Swap the DI binding in the module's service-collection extension:
   ```csharp
   services.AddScoped<IInboxStore, EfCoreInboxStore<MyDbContext>>();
   ```

For the single-transaction guarantee to hold, the handler must use the
same `DbContext` the inbox store was registered with. In Yumney every
module's projection / event-store handlers resolve the same `Scoped`
`DbContext` as the inbox store, so the transaction wraps both writes
naturally.
