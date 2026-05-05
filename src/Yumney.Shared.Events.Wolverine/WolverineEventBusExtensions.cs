using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using SmartSolutionsLab.Yumney.Shared.Events;
using SmartSolutionsLab.Yumney.Shared.Guards;
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
		// Aspire's AddRabbitMQ("messaging") in the AppHost wires this in both
		// run and publish mode. A null/empty value here means misconfiguration
		// — fail fast at composition time so the deploy reports the error
		// before any traffic hits the API and silently no-ops every publish.
		var connectionString = builder.Configuration.GetConnectionString("messaging");
		Ensure.That(connectionString)
			.IsNotNullOrWhiteSpace();

		builder.Host.UseWolverine(opts =>
		{
			opts.Discovery.DisableConventionalDiscovery();

			opts.UseRabbitMq(new Uri(connectionString!))
				.AutoProvision()
				.UseConventionalRouting();

			foreach (var assembly in eventHandlerAssemblies)
			{
				RegisterBusEventConsumers(opts, assembly, typeof(IIntegrationEventHandler<>), typeof(IntegrationEventConsumer<>));
				RegisterBusEventConsumers(opts, assembly, typeof(IModuleEventHandler<>), typeof(ModuleEventConsumer<>));
			}
		});

		builder.Services.AddScoped<IEventBus, WolverineEventBus>();
		builder.Services.TryAddScoped<IInboxStore, NoOpInboxStore>();

		return builder;
	}

	private static void RegisterBusEventConsumers(
		WolverineOptions opts,
		Assembly assembly,
		Type openHandlerInterface,
		Type openConsumerType)
	{
		var eventTypes = assembly.GetTypes()
			.SelectMany(type => type.GetInterfaces())
			.Where(iface => iface.IsGenericType && iface.GetGenericTypeDefinition() == openHandlerInterface)
			.Select(iface => iface.GetGenericArguments()[0])
			.Distinct();

		foreach (var eventType in eventTypes)
		{
			opts.Discovery.IncludeType(openConsumerType.MakeGenericType(eventType));
		}
	}
}
