using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace SmartSolutionsLab.Yumney.Shared.Events;

public static class BusEventHandlerRegistration
{
	public static IServiceCollection AddBusEventHandlersFromAssemblyContaining<T>(this IServiceCollection services) =>
		services.AddBusEventHandlers(typeof(T).Assembly);

	public static IServiceCollection AddBusEventHandlers(this IServiceCollection services, Assembly assembly)
	{
		Type[] openHandlers = [typeof(IIntegrationEventHandler<>), typeof(IModuleEventHandler<>)];

		var implementations = assembly.GetTypes()
			.Where(type => type is { IsAbstract: false, IsInterface: false })
			.Select(type => (Implementation: type, Interfaces: type.GetInterfaces()
				.Where(iface => iface.IsGenericType && openHandlers.Contains(iface.GetGenericTypeDefinition()))
				.ToArray()))
			.Where(pair => pair.Interfaces.Length > 0);

		// Register the concrete handler once as scoped, then point every
		// IIntegrationEventHandler<TEvent> / IModuleEventHandler<TEvent> at that
		// single instance per scope. Without the concrete registration, a handler
		// that consumes N events would be instantiated N times per scope.
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
