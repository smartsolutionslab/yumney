using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using SmartSolutionsLab.Yumney.Shared.Events;
using SmartSolutionsLab.Yumney.Shared.Persistence.EventStore;

namespace SmartSolutionsLab.Yumney.Shared.Persistence;

/// <summary>
/// EF Core-backed <see cref="IInboxStore"/>. Each <see cref="BeginAsync"/>
/// call opens a database transaction on the supplied context and stages an
/// <see cref="InboxMessage"/> row when the (messageId, consumerName) pair
/// is not already present. The caller (event consumer) commits the scope
/// only after the handler succeeds, so the inbox row and the handler's
/// writes share a single transactional fate. On handler failure the scope
/// rolls back, leaving the inbox empty for the next delivery to retry.
/// </summary>
/// <typeparam name="TContext">The DbContext that holds the InboxMessages set.</typeparam>
public sealed class EfCoreInboxStore<TContext>(TContext context) : IInboxStore
	where TContext : DbContext
{
	public async Task<IInboxScope> BeginAsync(Guid messageId, string consumerName, CancellationToken cancellationToken = default)
	{
		var transaction = await context.Database.BeginTransactionAsync(cancellationToken);

		var alreadyProcessed = await context.Set<InboxMessage>()
			.AsNoTracking()
			.AnyAsync(message => message.MessageId == messageId && message.ConsumerName == consumerName, cancellationToken);

		if (alreadyProcessed)
		{
			return new EfCoreInboxScope(context, transaction, shouldProcess: false, stagedEntry: null);
		}

		var entry = context.Set<InboxMessage>().Add(new InboxMessage
		{
			MessageId = messageId,
			ConsumerName = consumerName,
		});

		return new EfCoreInboxScope(context, transaction, shouldProcess: true, stagedEntry: entry.Entity);
	}

	private sealed class EfCoreInboxScope(
		TContext context,
		IDbContextTransaction transaction,
		bool shouldProcess,
		InboxMessage? stagedEntry) : IInboxScope
	{
		private bool finalized;

		public bool ShouldProcess => shouldProcess;

		public async Task CommitAsync(CancellationToken cancellationToken = default)
		{
			if (finalized) return;
			finalized = true;

			await context.SaveChangesAsync(cancellationToken);
			await transaction.CommitAsync(cancellationToken);
		}

		public async Task RollbackAsync(CancellationToken cancellationToken = default)
		{
			if (finalized) return;
			finalized = true;

			DetachStagedEntry();
			await transaction.RollbackAsync(cancellationToken);
		}

		public bool IsDuplicateInboxViolation(Exception exception)
		{
			return exception is DbUpdateException dbUpdate && dbUpdate.IsUniqueViolation();
		}

		public async ValueTask DisposeAsync()
		{
			if (!finalized)
			{
				finalized = true;
				DetachStagedEntry();
				await transaction.RollbackAsync();
			}

			await transaction.DisposeAsync();
		}

		private void DetachStagedEntry()
		{
			if (stagedEntry is null) return;

			var tracked = context.ChangeTracker.Entries<InboxMessage>()
				.FirstOrDefault(entry => ReferenceEquals(entry.Entity, stagedEntry));

			if (tracked is { } trackedEntry)
			{
				trackedEntry.State = EntityState.Detached;
			}
		}
	}
}
