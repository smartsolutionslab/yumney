using SmartSolutionsLab.Yumney.Shared.Common;

namespace SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingList.Events;

public sealed record ListItemCategoryChanged(
	ShoppingListItemIdentifier ItemId,
	IngredientCategory Category) : DomainEvent;
