using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using SmartSolutionsLab.Yumney.Shared.Abstractions;
using SmartSolutionsLab.Yumney.Shared.Events;

namespace SmartSolutionsLab.Yumney.Shared.Persistence;

// Registered as a singleton so it can be added to a pooled / root-provider-built
// DbContextOptions chain (Wolverine.EntityFrameworkCore's AddDbContextWithWolverineIntegration
// resolves interceptors against the root provider — a scoped interceptor throws
// InvalidOperationException there). A fresh DI scope is created per SaveChanges
// to resolve IDomainEventDispatcher, so handlers still see request-scoped
// services if the host has a current scope.
public sealed class DomainEventDispatchInterceptor(IServiceScopeFactory scopeFactory) : SaveChangesInterceptor
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

		if (domainEvents.Count == 0) return;

		await using var scope = scopeFactory.CreateAsyncScope();
		var dispatcher = scope.ServiceProvider.GetRequiredService<IDomainEventDispatcher>();
		await dispatcher.DispatchAsync(domainEvents, cancellationToken);
	}
}
