using Microsoft.Extensions.DependencyInjection;

namespace SmartSolutionsLab.Yumney.Shared.Events;

/// <summary>
/// Extension methods for registering in-process event dispatching.
/// </summary>
public static class EventingServiceCollectionExtensions
{
	/// <summary>
	/// Registers the in-process <see cref="IDomainEventDispatcher"/>. Always
	/// required — domain events are intra-module and never go over the wire,
	/// even when integration events use a distributed bus.
	/// </summary>
	/// <param name="services">The service collection.</param>
	/// <returns>The service collection for chaining.</returns>
	public static IServiceCollection AddInProcessDomainEventDispatcher(this IServiceCollection services)
	{
		services.AddScoped<IDomainEventDispatcher, InProcessDomainEventDispatcher>();

		return services;
	}

	/// <summary>
	/// Registers the in-process <see cref="IEventBus"/>. Use this only when
	/// the host does NOT register a distributed event bus (Wolverine etc.) —
	/// a later <c>IEventBus</c> registration supersedes this one for
	/// cross-module integration events.
	/// </summary>
	/// <param name="services">The service collection.</param>
	/// <returns>The service collection for chaining.</returns>
	public static IServiceCollection AddInProcessEventBus(this IServiceCollection services)
	{
		services.AddScoped<IEventBus, InProcessEventBus>();

		return services;
	}
}
