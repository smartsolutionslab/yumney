using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shared.Events;
using Wolverine;
using Wolverine.RabbitMQ;

namespace SmartSolutionsLab.Yumney.Shared.Events.Wolverine;

/// <summary>
/// Extension methods for registering Wolverine-based event bus with RabbitMQ transport.
/// </summary>
public static class WolverineEventBusExtensions
{
	/// <summary>
	/// Registers Wolverine with RabbitMQ as the integration event bus.
	/// Domain events remain in-process via InProcessDomainEventDispatcher.
	/// Integration events are published/consumed via RabbitMQ.
	/// </summary>
	/// <param name="builder">The host application builder.</param>
	/// <param name="eventHandlerAssemblies">Assemblies to scan for IIntegrationEventHandler implementations.</param>
	/// <returns>The host application builder for chaining.</returns>
	public static WebApplicationBuilder AddWolverineEventBus(
		this WebApplicationBuilder builder,
		params Assembly[] eventHandlerAssemblies)
	{
		var connectionString = builder.Configuration.GetConnectionString("messaging");

		builder.Host.UseWolverine(opts =>
		{
			opts.Discovery.DisableConventionalDiscovery();

			if (connectionString.HasValue())
			{
				opts.UseRabbitMq(new Uri(connectionString!))
					.AutoProvision()
					.UseConventionalRouting();
			}

			foreach (var assembly in eventHandlerAssemblies)
			{
				RegisterIntegrationEventHandlers(opts, assembly);
			}
		});

		builder.Services.AddScoped<IEventBus, WolverineEventBus>();
		builder.Services.TryAddScoped<IInboxStore, NoOpInboxStore>();

		return builder;
	}

	private static void RegisterIntegrationEventHandlers(WolverineOptions opts, Assembly assembly)
	{
		var handlerInterfaceType = typeof(IIntegrationEventHandler<>);
		var handlerType = typeof(IntegrationEventConsumer<>);

		var eventTypes = assembly.GetTypes()
			.SelectMany(type => type.GetInterfaces())
			.Where(iface => iface.IsGenericType && iface.GetGenericTypeDefinition() == handlerInterfaceType)
			.Select(iface => iface.GetGenericArguments()[0])
			.Distinct();

		foreach (var eventType in eventTypes)
		{
			opts.Discovery.IncludeType(handlerType.MakeGenericType(eventType));
		}
	}
}
