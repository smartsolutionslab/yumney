using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace SmartSolutionsLab.Yumney.Shared.Events;

/// <summary>
/// Extension methods for registering in-process event dispatching.
/// </summary>
public static class EventingServiceCollectionExtensions
{
	/// <param name="services">The service collection.</param>
	extension(IServiceCollection services)
	{
		/// <summary>
		/// Registers the in-process <see cref="IDomainEventDispatcher"/>. Always
		/// required — domain events are intra-module and never go over the wire,
		/// even when integration events use a distributed bus.
		/// </summary>
		/// <returns>The service collection for chaining.</returns>
		public IServiceCollection AddInProcessDomainEventDispatcher()
		{
			// Counters used by the dispatcher and the in-process bus. Registered
			// as singleton because Counter<T> instances from a Meter are
			// thread-safe and meant to be reused across the process lifetime.
			services.TryAddSingleton<EventMetrics>();
			services.AddScoped<IDomainEventDispatcher, InProcessDomainEventDispatcher>();

			return services;
		}

		/// <summary>
		/// Registers the in-process <see cref="IEventBus"/>. Use this only when
		/// the host does NOT register a distributed event bus (Wolverine etc.) —
		/// a later <c>IEventBus</c> registration supersedes this one for
		/// cross-module integration events.
		/// </summary>
		/// <returns>The service collection for chaining.</returns>
		public IServiceCollection AddInProcessEventBus()
		{
			services.TryAddSingleton<EventMetrics>();
			services.AddScoped<IEventBus, InProcessEventBus>();

			return services;
		}
	}
}
