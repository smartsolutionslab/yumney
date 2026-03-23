using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
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
    /// </summary>
    /// <returns></returns>
    public static IServiceCollection AddMassTransitEventBus(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddMassTransit(x =>
        {
            x.SetKebabCaseEndpointNameFormatter();

            x.UsingRabbitMq((context, cfg) =>
            {
                var connectionString = configuration.GetConnectionString("messaging");
                if (!string.IsNullOrEmpty(connectionString))
                {
                    cfg.Host(connectionString);
                }

                cfg.ConfigureEndpoints(context);
            });
        });

        services.AddScoped<IEventBus, MassTransitEventBus>();

        return services;
    }
}
