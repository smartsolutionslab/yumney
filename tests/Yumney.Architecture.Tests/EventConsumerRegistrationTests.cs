using System.Reflection;
using FluentAssertions;
using SmartSolutionsLab.Yumney.Shared.Events;
using Xunit;

namespace SmartSolutionsLab.Yumney.Architecture.Tests;

public class EventConsumerRegistrationTests
{
	private static readonly string[] InfrastructureModules = ["Recipes", "Shopping", "Users", "MealPlan"];

	[Fact]
	public void EveryIntegrationEvent_HasAtLeastOneHandler()
	{
		var assemblies = InfrastructureModules
			.Select(module => Assembly.Load($"Yumney.{module}.Infrastructure"))
			.ToList();

		var integrationEventType = typeof(IIntegrationEvent);
		var handlerInterfaceType = typeof(IIntegrationEventHandler<>);

		var integrationEvents = assemblies
			.SelectMany(assembly => assembly.GetTypes())
			.Where(type => integrationEventType.IsAssignableFrom(type) && type is { IsClass: true, IsAbstract: false })
			.ToList();

		var handledEventTypes = assemblies
			.SelectMany(assembly => assembly.GetTypes())
			.SelectMany(type => type.GetInterfaces())
			.Where(iface => iface.IsGenericType && iface.GetGenericTypeDefinition() == handlerInterfaceType)
			.Select(iface => iface.GetGenericArguments()[0])
			.ToHashSet();

		integrationEvents.Should().NotBeEmpty("at least one integration event must exist across modules");

		var missing = integrationEvents
			.Where(eventType => !handledEventTypes.Contains(eventType))
			.Select(eventType => eventType.FullName)
			.ToList();

		missing.Should().BeEmpty(
			"every IIntegrationEvent must have at least one IIntegrationEventHandler<T> implementation. " +
			"Events without handlers will silently drop at runtime.");
	}

	[Fact]
	public void EveryIntegrationEventHandler_HandlesAKnownIntegrationEvent()
	{
		var assemblies = InfrastructureModules
			.Select(module => Assembly.Load($"Yumney.{module}.Infrastructure"))
			.ToList();

		var integrationEventType = typeof(IIntegrationEvent);
		var handlerInterfaceType = typeof(IIntegrationEventHandler<>);

		var handlerEventTargets = assemblies
			.SelectMany(assembly => assembly.GetTypes())
			.SelectMany(type => type.GetInterfaces())
			.Where(iface => iface.IsGenericType && iface.GetGenericTypeDefinition() == handlerInterfaceType)
			.Select(iface => iface.GetGenericArguments()[0])
			.Distinct()
			.ToList();

		var nonEventHandlerTargets = handlerEventTargets
			.Where(type => !integrationEventType.IsAssignableFrom(type))
			.Select(type => type.FullName)
			.ToList();

		nonEventHandlerTargets.Should().BeEmpty(
			"every IIntegrationEventHandler<T> must target a type implementing IIntegrationEvent");
	}
}
