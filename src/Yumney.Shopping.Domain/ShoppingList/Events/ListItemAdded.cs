using SmartSolutionsLab.Yumney.Shared.Abstractions;

namespace SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingList.Events;

public sealed record ListItemAdded(
	ShoppingListItemIdentifier ItemId,
	ItemName Name,
	Quantity? Quantity,
	IngredientCategory? Category = null) : DomainEvent;
