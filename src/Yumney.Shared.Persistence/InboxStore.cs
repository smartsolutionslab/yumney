using Microsoft.EntityFrameworkCore;
using SmartSolutionsLab.Yumney.Shared.Events;
using SmartSolutionsLab.Yumney.Shared.Persistence.EventStore;

namespace SmartSolutionsLab.Yumney.Shared.Persistence;

/// <summary>
/// EF Core-backed <see cref="IInboxStore"/>. Wraps the
/// begin → dedup-check → handler → save → commit sequence in
/// <see cref="Microsoft.EntityFrameworkCore.IExecutionStrategy.ExecuteAsync(Func{Task})"/>
/// so the configured retrying execution strategy can replay the whole unit
/// on transient failures. The handler's writes (queued through the same
/// <typeparamref name="TContext"/>) and the inbox row share a single
/// transaction — a handler exception rolls both back, and a redelivery
/// re-runs the handler from scratch.
/// </summary>
/// <typeparam name="TContext">DbContext that owns the InboxMessages set.</typeparam>
public sealed class InboxStore<TContext>(TContext context) : IInboxStore
	where TContext : DbContext
{
	public async Task<InboxOutcome> TryProcessAsync(Guid messageId, string consumerName, Func<CancellationToken, Task> handler, CancellationToken cancellationToken = default)
	{
		var strategy = context.Database.CreateExecutionStrategy();

		try
		{
			return await strategy.ExecuteAsync(async () =>
			{
				await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);

				var alreadyProcessed = await context.Set<InboxMessage>()
					.AsNoTracking()
					.AnyAsync(message => message.MessageId == messageId && message.ConsumerName == consumerName, cancellationToken);

				if (alreadyProcessed) return InboxOutcome.AlreadyProcessed;

				context.Set<InboxMessage>().Add(new InboxMessage
				{
					MessageId = messageId,
					ConsumerName = consumerName,
				});

				await handler(cancellationToken);
				await context.SaveChangesAsync(cancellationToken);
				await transaction.CommitAsync(cancellationToken);
				return InboxOutcome.Processed;
			});
		}
		catch (DbUpdateException dbUpdate) when (dbUpdate.IsUniqueViolation())
		{
			// A concurrent peer committed the same (messageId, consumerName)
			// pair between our pre-check and our save. The transaction (and
			// thus the handler's writes) rolled back; the peer's row is now
			// the source of truth.
			return InboxOutcome.DuplicateRace;
		}
	}
}
