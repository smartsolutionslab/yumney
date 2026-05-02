using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace SmartSolutionsLab.Yumney.Shared.Events;

public static class IntegrationEventHandlerRegistration
{
	public static IServiceCollection AddIntegrationEventHandlersFromAssemblyContaining<T>(this IServiceCollection services) =>
		services.AddIntegrationEventHandlers(typeof(T).Assembly);

	public static IServiceCollection AddIntegrationEventHandlers(this IServiceCollection services, Assembly assembly)
	{
		var openHandler = typeof(IIntegrationEventHandler<>);

		var implementations = assembly.GetTypes()
			.Where(type => type is { IsAbstract: false, IsInterface: false })
			.Select(type => (Implementation: type, Interfaces: type.GetInterfaces()
				.Where(iface => iface.IsGenericType && iface.GetGenericTypeDefinition() == openHandler)
				.ToArray()))
			.Where(pair => pair.Interfaces.Length > 0);

		// Register the concrete handler once as scoped, then point every
		// IIntegrationEventHandler<TEvent> at that single instance per scope.
		// Without the concrete registration, a handler that consumes N events
		// would be instantiated N times per scope.
		foreach (var (implementation, interfaces) in implementations)
		{
			services.AddScoped(implementation);
			foreach (var iface in interfaces)
			{
				services.AddScoped(iface, sp => sp.GetRequiredService(implementation));
			}
		}

		return services;
	}
}
