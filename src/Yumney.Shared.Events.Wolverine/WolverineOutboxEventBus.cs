using Microsoft.EntityFrameworkCore;
using Wolverine.EntityFrameworkCore;

namespace SmartSolutionsLab.Yumney.Shared.Events.Wolverine;

/// <summary>
/// IEventBus implementation that stages messages on Wolverine's typed
/// <see cref="IDbContextOutbox{T}"/> instead of publishing immediately. The
/// staged rows persist alongside the next <c>SaveChangesAsync</c> call on the
/// owning DbContext, so handlers that produce both entity writes and bus
/// messages can call <c>PublishAsync</c> *before* <c>SaveChangesAsync</c> and
/// have the two commit in a single Postgres transaction. Replacement for
/// <see cref="WolverineEventBus"/> in modules whose state-based handlers
/// would otherwise dual-write.
/// </summary>
/// <typeparam name="TContext">The DbContext that owns the outbox tables.</typeparam>
public sealed class WolverineOutboxEventBus<TContext>(IDbContextOutbox<TContext> outbox) : IEventBus
	where TContext : DbContext
{
	public async Task PublishAsync<TEvent>(TEvent busEvent, CancellationToken cancellationToken = default)
		where TEvent : IBusEvent
	{
		// outbox.PublishAsync stages the message on Wolverine's outgoing
		// envelope tables. The row only persists when the next SaveChangesAsync
		// runs (Wolverine's EF Core interceptor flushes staged rows in the
		// same transaction). Handlers using this bus must call PublishAsync
		// BEFORE SaveChangesAsync — otherwise the message is lost.
		await outbox.PublishAsync(busEvent);
	}
}
