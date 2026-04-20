# Integration-event inbox

Every `IIntegrationEventHandler<T>` invocation goes through an `IInboxStore`
before the handler runs. The store's `TryMarkProcessedAsync(messageId,
consumerName)` is expected to persist the pair atomically and return
`true` only when the pair was not already present. On `false`, the
generic consumer skips the handler, preventing redelivered or duplicate
messages from replaying side effects.

## Default: `NoOpInboxStore`

The Wolverine bus registers `NoOpInboxStore` by default — every call
returns `true`, matching pre-inbox behaviour. No schema change is
required to adopt the abstraction; modules opt in to real deduplication
on their own timeline.

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

`EfCoreInboxStore<TContext>` relies on the composite unique key
`(MessageId, ConsumerName)` defined by `InboxMessageConfiguration`. A
duplicate insert raises `DbUpdateException`, which the store translates
into the `false` return the consumer uses to skip the handler.
