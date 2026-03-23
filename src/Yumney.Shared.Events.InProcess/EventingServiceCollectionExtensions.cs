using Microsoft.Extensions.DependencyInjection;

namespace SmartSolutionsLab.Yumney.Shared.Events;

/// <summary>
/// Extension methods for registering in-process event dispatching.
/// </summary>
public static class EventingServiceCollectionExtensions
{
    /// <summary>
    /// Registers in-process domain event dispatcher and in-process event bus.
    /// When using a distributed event bus (e.g. MassTransit), call this first for domain events,
    /// then register the distributed IEventBus which will override the in-process one.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddInProcessEventBus(this IServiceCollection services)
    {
        services.AddScoped<IDomainEventDispatcher, InProcessDomainEventDispatcher>();
        services.AddScoped<IEventBus, InProcessEventBus>();

        return services;
    }
}
