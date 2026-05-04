using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using SmartSolutionsLab.Yumney.Shared.Abstractions;
using SmartSolutionsLab.Yumney.Shared.Events;
using SmartSolutionsLab.Yumney.Shared.Outcomes;

namespace SmartSolutionsLab.Yumney.Shared.Persistence;

public sealed class DomainEventDispatchInterceptor(IDomainEventDispatcher dispatcher) : SaveChangesInterceptor
{
	public override async ValueTask<int> SavedChangesAsync(SaveChangesCompletedEventData eventData, int result, CancellationToken cancellationToken = default)
	{
		if (eventData.Context is not null)
		{
			await DispatchDomainEventsAsync(eventData.Context, cancellationToken);
		}

		return await base.SavedChangesAsync(eventData, result, cancellationToken);
	}

	private async Task DispatchDomainEventsAsync(DbContext context, CancellationToken cancellationToken)
	{
		var entities = context.ChangeTracker
			.Entries<IHasDomainEvents>()
			.Where(entry => entry.Entity.DomainEvents.Count > 0)
			.Select(entry => entry.Entity)
			.ToList();

		var domainEvents = entities
			.SelectMany(entity => entity.DomainEvents)
			.ToList();

		foreach (var entity in entities)
		{
			entity.ClearDomainEvents();
		}

		if (domainEvents.Count > 0) await dispatcher.DispatchAsync(domainEvents, cancellationToken);
	}
}
