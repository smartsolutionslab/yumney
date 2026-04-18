using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shared.CQRS;
using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingList;

namespace SmartSolutionsLab.Yumney.Shopping.Application.Commands;

/// <summary>
/// Remove an item from the shopping list (via chat command or UI).
/// </summary>
public sealed record RemoveShoppingItemCommand(
	ItemName ItemName,
	Quantity? Quantity,
	RemovalReason? Reason) : ICommand<Result>;
