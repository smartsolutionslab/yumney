# Integration-event inbox

Every `IIntegrationEventHandler<T>` (and `IModuleEventHandler<T>`)
invocation runs inside an `IInboxScope` opened by the registered
`IInboxStore`. The scope owns a transaction wrapping both the inbox
`(messageId, consumerName)` row and the handler's own writes — the row
only commits if the handler succeeds, and a thrown handler rolls the row
back so a redelivery can retry the work cleanly.

## Default: `NoOpInboxStore`

The Wolverine bus registers `NoOpInboxStore` by default. Its scope
always reports `ShouldProcess = true` and commit / rollback are no-ops,
matching pre-inbox behaviour. No schema change is required to adopt the
abstraction; modules opt in to real deduplication on their own timeline.

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

## Atomicity contract

`EfCoreInboxStore<TContext>` opens a database transaction on the supplied
context inside `BeginAsync`. While that transaction is open it pre-checks
the inbox; if the row is already present the scope reports
`ShouldProcess = false` and the consumer skips the handler.

If the row is absent the scope stages the `InboxMessage` add and hands
control to the consumer, which invokes the handler and calls
`scope.CommitAsync()`. Commit flushes pending changes on the same
context (handler writes + the staged inbox row) and commits the
transaction in one shot. Handler exceptions cause `scope.RollbackAsync()`,
which discards the staged row alongside any partial handler writes — the
next delivery sees a clean inbox and retries.

For this to work the handler must use the same `DbContext` the inbox
store was registered with. In Yumney every module's projection /
event-store handlers resolve the same `Scoped` DbContext as the inbox
store, so the transaction wraps both writes naturally.

A concurrent peer that commits the same `(messageId, consumerName)` row
between our pre-check and our commit shows up as a unique-constraint
violation. The consumer detects this via `scope.IsDuplicateInboxViolation`
and treats it as a duplicate — rollback, log, skip, no rethrow — so
Wolverine does not retry an already-completed message.
