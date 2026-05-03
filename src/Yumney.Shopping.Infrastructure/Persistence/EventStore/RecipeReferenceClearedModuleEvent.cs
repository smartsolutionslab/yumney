using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingList.Events;

namespace SmartSolutionsLab.Yumney.Shopping.Infrastructure.Persistence.EventStore;

#pragma warning disable SA1649
public sealed record RecipeReferenceClearedModuleEvent(
	string OwnerId,
	Guid AggregateId,
	RecipeReferenceCleared Inner) : ShoppingListModuleEvent(OwnerId, AggregateId);
