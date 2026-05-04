using SmartSolutionsLab.Yumney.Shared.Abstractions;
using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingList;

namespace SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingLedger.Events;

public sealed record ShoppingItemAdded(
	ItemName ItemName,
	Quantity Quantity,
	ItemSource Source) : DomainEvent;
