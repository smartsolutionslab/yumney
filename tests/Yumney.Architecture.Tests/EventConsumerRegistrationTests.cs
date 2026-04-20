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
			.Select(m => Assembly.Load($"Yumney.{m}.Infrastructure"))
			.ToList();

		var integrationEventType = typeof(IIntegrationEvent);
		var handlerInterfaceType = typeof(IIntegrationEventHandler<>);

		var integrationEvents = assemblies
			.SelectMany(a => a.GetTypes())
			.Where(t => integrationEventType.IsAssignableFrom(t) && t is { IsClass: true, IsAbstract: false })
			.ToList();

		var handledEventTypes = assemblies
			.SelectMany(a => a.GetTypes())
			.SelectMany(t => t.GetInterfaces())
			.Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == handlerInterfaceType)
			.Select(i => i.GetGenericArguments()[0])
			.ToHashSet();

		integrationEvents.Should().NotBeEmpty("at least one integration event must exist across modules");

		var missing = integrationEvents
			.Where(e => !handledEventTypes.Contains(e))
			.Select(e => e.FullName)
			.ToList();

		missing.Should().BeEmpty(
			"every IIntegrationEvent must have at least one IIntegrationEventHandler<T> implementation. " +
			"Events without handlers will silently drop at runtime.");
	}

	[Fact]
	public void EveryIntegrationEventHandler_HandlesAKnownIntegrationEvent()
	{
		var assemblies = InfrastructureModules
			.Select(m => Assembly.Load($"Yumney.{m}.Infrastructure"))
			.ToList();

		var integrationEventType = typeof(IIntegrationEvent);
		var handlerInterfaceType = typeof(IIntegrationEventHandler<>);

		var handlerEventTargets = assemblies
			.SelectMany(a => a.GetTypes())
			.SelectMany(t => t.GetInterfaces())
			.Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == handlerInterfaceType)
			.Select(i => i.GetGenericArguments()[0])
			.Distinct()
			.ToList();

		var nonEventHandlerTargets = handlerEventTargets
			.Where(t => !integrationEventType.IsAssignableFrom(t))
			.Select(t => t.FullName)
			.ToList();

		nonEventHandlerTargets.Should().BeEmpty(
			"every IIntegrationEventHandler<T> must target a type implementing IIntegrationEvent");
	}
}
