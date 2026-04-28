using SmartSolutionsLab.Yumney.Shared.Common;

namespace SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingList.Events;

public sealed record ListItemAdded(
	ShoppingListItemIdentifier ItemId,
	ItemName Name,
	Quantity? Quantity) : DomainEvent;
