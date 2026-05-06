using SmartSolutionsLab.Yumney.Shared.Abstractions;
using SmartSolutionsLab.Yumney.Shared.Events;
using SmartSolutionsLab.Yumney.Shared.Events.CrossModule;
using SmartSolutionsLab.Yumney.Shared.Persistence.EventStore;
using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingList.Events;

namespace SmartSolutionsLab.Yumney.Shopping.Infrastructure.Persistence.EventStore.CrossModule;

internal sealed class ShoppingListCreatedMapper : ICrossModuleEventMapper
{
	public Type DomainEventType => typeof(ShoppingListCreated);

	public IIntegrationEvent? TryMap(IReadOnlyList<object> context, IDomainEvent domainEvent)
	{
		if (domainEvent is not ShoppingListCreated created) return null;
		return new ShoppingListCreatedCrossModuleIntegrationEvent(
			(string)context[0],
			(Guid)context[1],
			created.Title.Value,
			created.RecipeReference?.Value,
			created.CreatedAt);
	}
}
