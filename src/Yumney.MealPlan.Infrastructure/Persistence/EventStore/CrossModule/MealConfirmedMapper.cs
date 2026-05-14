using SmartSolutionsLab.Yumney.MealPlan.Domain.WeeklyPlan.Events;
using SmartSolutionsLab.Yumney.Shared.Abstractions;
using SmartSolutionsLab.Yumney.Shared.Events;
using SmartSolutionsLab.Yumney.Shared.Events.Contracts;
using SmartSolutionsLab.Yumney.Shared.Persistence.EventStore;

namespace SmartSolutionsLab.Yumney.MealPlan.Infrastructure.Persistence.EventStore.CrossModule;

internal sealed class MealConfirmedMapper : ICrossModuleEventMapper
{
	public Type DomainEventType => typeof(MealMarkedAsCooked);

	public IIntegrationEvent? TryMap(IReadOnlyList<object> context, IDomainEvent domainEvent)
	{
		if (domainEvent is not MealMarkedAsCooked cooked) return null;
		if (cooked.Recipe is null) return null;

		return new MealConfirmedIntegrationEvent(
			(string)context[0],
			cooked.Recipe.Identifier.Value,
			cooked.Servings.Value,
			cooked.Ingredients
				.Select(ingredient => new MealConfirmedIngredient(ingredient.Name, ingredient.Quantity, ingredient.Unit))
				.ToList());
	}
}
