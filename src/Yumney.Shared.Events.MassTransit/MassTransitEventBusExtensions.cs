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
	/// <param name="eventHandlerAssemblies">Assemblies to scan for IIntegrationEventHandler implementations.</param>
	/// <returns>The service collection for chaining.</returns>
	public static IServiceCollection AddMassTransitEventBus(
		this IServiceCollection services,
		IConfiguration configuration,
		params Assembly[] eventHandlerAssemblies)
	{
		services.AddMassTransit(x =>
		{
			x.SetKebabCaseEndpointNameFormatter();

			foreach (var assembly in eventHandlerAssemblies)
			{
				x.AddConsumers(assembly);
				RegisterIntegrationEventConsumers(x, assembly);
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

	private static void RegisterIntegrationEventConsumers(IRegistrationConfigurator configurator, Assembly assembly)
	{
		var handlerInterfaceType = typeof(IIntegrationEventHandler<>);
		var consumerType = typeof(IntegrationEventConsumer<>);

		var eventTypes = assembly.GetTypes()
			.SelectMany(t => t.GetInterfaces())
			.Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == handlerInterfaceType)
			.Select(i => i.GetGenericArguments()[0])
			.Distinct();

		foreach (var eventType in eventTypes)
		{
			configurator.AddConsumer(consumerType.MakeGenericType(eventType));
		}
	}
}
