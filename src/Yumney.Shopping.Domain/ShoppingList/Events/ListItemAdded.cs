using SmartSolutionsLab.Yumney.Shared.Abstractions;
using SmartSolutionsLab.Yumney.Shared.Quantities;

namespace SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingList.Events;

public sealed record ListItemAdded(
	ShoppingListItemIdentifier ItemId,
	ItemName Name,
	Quantity? Quantity,
	IngredientCategory? Category = null) : DomainEvent;
