using System.Reflection;
using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shared.Events;

namespace SmartSolutionsLab.Yumney.Shared.Events.MassTransit;

/// <summary>
/// Extension methods for registering MassTransit-based event bus.
/// </summary>
public static class MassTransitEventBusExtensions
{
    /// <summary>
    /// Registers MassTransit with RabbitMQ as the integration event bus.
    /// Domain events remain in-process via InProcessDomainEventDispatcher.
    /// Integration events are published/consumed via RabbitMQ.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The application configuration.</param>
    /// <param name="assemblies">Assemblies to scan for IIntegrationEventHandler implementations.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddMassTransitEventBus(
        this IServiceCollection services,
        IConfiguration configuration,
        params Assembly[] assemblies)
    {
        services.AddMassTransit(x =>
        {
            x.SetKebabCaseEndpointNameFormatter();

            foreach (var assembly in assemblies)
            {
                x.AddConsumers(assembly);
            }

            x.UsingRabbitMq((context, cfg) =>
            {
                var connectionString = configuration.GetConnectionString("messaging");
                if (connectionString.HasValue()) cfg.Host(connectionString);

                cfg.ConfigureEndpoints(context);
            });
        });

        services.AddScoped<IEventBus, MassTransitEventBus>();

        return services;
    }
}
