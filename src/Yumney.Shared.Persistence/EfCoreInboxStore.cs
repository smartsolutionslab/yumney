using Microsoft.EntityFrameworkCore;
using SmartSolutionsLab.Yumney.Shared.Events;

namespace SmartSolutionsLab.Yumney.Shared.Persistence;

/// <summary>
/// EF Core-backed <see cref="IInboxStore"/>. Relies on a composite unique
/// constraint on (MessageId, ConsumerName) in the target context: an
/// insert that violates the constraint is translated into a <c>false</c>
/// return so the caller can skip the handler.
/// </summary>
/// <typeparam name="TContext">The DbContext that holds the InboxMessages set.</typeparam>
public sealed class EfCoreInboxStore<TContext>(TContext context) : IInboxStore
	where TContext : DbContext
{
	public async Task<bool> TryMarkProcessedAsync(Guid messageId, string consumerName, CancellationToken cancellationToken = default)
	{
		var set = context.Set<InboxMessage>();
		set.Add(new InboxMessage { MessageId = messageId, ConsumerName = consumerName });

		try
		{
			await context.SaveChangesAsync(cancellationToken);
			return true;
		}
		catch (DbUpdateException)
		{
			// Likely a unique-constraint violation because the pair is already present.
			// Detach the failed entity so subsequent saves on this context are not affected.
			foreach (var entry in context.ChangeTracker.Entries<InboxMessage>().ToList())
			{
				if (entry.Entity.MessageId == messageId && entry.Entity.ConsumerName == consumerName)
				{
					entry.State = EntityState.Detached;
				}
			}

			return false;
		}
	}
}
