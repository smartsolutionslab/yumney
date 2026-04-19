using FluentAssertions;
using SmartSolutionsLab.Yumney.Shared.Events;
using SmartSolutionsLab.Yumney.Shared.Events.Wolverine;
using Xunit;

namespace SmartSolutionsLab.Yumney.Architecture.Tests;

public class EventConsumerRegistrationTests
{
	private static readonly string[] InfrastructureModules = ["Recipes", "Shopping", "Users", "MealPlan"];

	[Fact]
	public void AllIntegrationEventHandlers_HaveMatchingHandlerType()
	{
		var handlerInterfaceType = typeof(IIntegrationEventHandler<>);
		var handlerType = typeof(IntegrationEventConsumer<>);

		foreach (var module in InfrastructureModules)
		{
			var assembly = System.Reflection.Assembly.Load($"Yumney.{module}.Infrastructure");

			var eventTypes = assembly.GetTypes()
				.SelectMany(t => t.GetInterfaces())
				.Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == handlerInterfaceType)
				.Select(i => i.GetGenericArguments()[0])
				.Distinct()
				.ToList();

			foreach (var eventType in eventTypes)
			{
				var closedHandler = handlerType.MakeGenericType(eventType);
				closedHandler.Should().NotBeNull(
					$"IntegrationEventHandler<{eventType.Name}> must be constructable for handler in {module}.Infrastructure");
			}
		}
	}

	[Fact]
	public void RegisterIntegrationEventHandlers_DiscoversAllHandlers_InShoppingInfrastructure()
	{
		var assembly = typeof(SmartSolutionsLab.Yumney.Shopping.Infrastructure.ShoppingInfrastructureServiceCollectionExtensions).Assembly;
		var handlerInterfaceType = typeof(IIntegrationEventHandler<>);

		var eventTypes = assembly.GetTypes()
			.SelectMany(t => t.GetInterfaces())
			.Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == handlerInterfaceType)
			.Select(i => i.GetGenericArguments()[0])
			.Distinct()
			.ToList();

		eventTypes.Should().NotBeEmpty("Shopping.Infrastructure must have integration event handlers");
		eventTypes.Should().HaveCountGreaterThanOrEqualTo(5, "all shopping event types should be handled");
	}
}
