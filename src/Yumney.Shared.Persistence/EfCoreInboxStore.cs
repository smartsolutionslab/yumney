using Microsoft.EntityFrameworkCore;
using SmartSolutionsLab.Yumney.Shared.Events;
using SmartSolutionsLab.Yumney.Shared.Persistence.EventStore;

namespace SmartSolutionsLab.Yumney.Shared.Persistence;

/// <summary>
/// EF Core-backed <see cref="IInboxStore"/>. Each <see cref="ProcessAsync"/>
/// call routes through the configured execution strategy so a single transient
/// failure (e.g., dropped Postgres connection) replays the entire begin → check
/// → handler → record sequence as one retriable unit. The handler runs inside
/// the same transaction as the inbox row insert, so a thrown handler rolls
/// the row back and a redelivery picks the message up again.
/// </summary>
/// <typeparam name="TContext">DbContext that holds the InboxMessages set.</typeparam>
public sealed class EfCoreInboxStore<TContext>(TContext context) : IInboxStore
	where TContext : DbContext
{
	public async Task<bool> ProcessAsync(
		Guid messageId,
		string consumerName,
		Func<Task> handler,
		CancellationToken cancellationToken = default)
	{
		var strategy = context.Database.CreateExecutionStrategy();
		var ranHandler = false;
		Exception? handlerFailure = null;

		await strategy.ExecuteAsync(async () =>
		{
			ranHandler = false;
			handlerFailure = null;

			await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);

			var alreadyProcessed = await context.Set<InboxMessage>()
				.AsNoTracking()
				.AnyAsync(
					message => message.MessageId == messageId && message.ConsumerName == consumerName,
					cancellationToken);

			if (alreadyProcessed)
			{
				await transaction.CommitAsync(cancellationToken);
				return;
			}

			var stagedEntry = context.Set<InboxMessage>().Add(new InboxMessage
			{
				MessageId = messageId,
				ConsumerName = consumerName,
			});

			try
			{
				await handler();
				await context.SaveChangesAsync(cancellationToken);
				await transaction.CommitAsync(cancellationToken);
				ranHandler = true;
			}
			catch (DbUpdateException dbUpdate) when (dbUpdate.IsUniqueViolation())
			{
				// Concurrent peer recorded this row first — treat as already-processed.
				stagedEntry.State = EntityState.Detached;
				await transaction.RollbackAsync(cancellationToken);
			}
			catch (Exception exception)
			{
				stagedEntry.State = EntityState.Detached;
				await transaction.RollbackAsync(cancellationToken);
				handlerFailure = exception;
			}
		});

		if (handlerFailure is not null) throw handlerFailure;
		return ranHandler;
	}
}
