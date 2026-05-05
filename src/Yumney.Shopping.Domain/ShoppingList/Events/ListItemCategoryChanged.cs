using SmartSolutionsLab.Yumney.Shared.Abstractions;
using SmartSolutionsLab.Yumney.Shared.Quantities;

namespace SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingList.Events;

public sealed record ListItemCategoryChanged(
	ShoppingListItemIdentifier ItemId,
	IngredientCategory Category) : DomainEvent;
