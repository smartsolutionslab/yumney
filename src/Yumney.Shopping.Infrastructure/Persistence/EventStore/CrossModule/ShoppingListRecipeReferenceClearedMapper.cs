using SmartSolutionsLab.Yumney.Shared.Abstractions;
using SmartSolutionsLab.Yumney.Shared.Events;
using SmartSolutionsLab.Yumney.Shared.Events.CrossModule;
using SmartSolutionsLab.Yumney.Shared.Persistence.EventStore;
using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingList.Events;

namespace SmartSolutionsLab.Yumney.Shopping.Infrastructure.Persistence.EventStore.CrossModule;

internal sealed class ShoppingListRecipeReferenceClearedMapper : ICrossModuleEventMapper
{
	public Type DomainEventType => typeof(RecipeReferenceCleared);

	public IIntegrationEvent? TryMap(IReadOnlyList<object> context, IDomainEvent domainEvent)
	{
		if (domainEvent is not RecipeReferenceCleared) return null;
		return new ShoppingListRecipeReferenceClearedCrossModuleIntegrationEvent(
			(string)context[0],
			(Guid)context[1]);
	}
}
