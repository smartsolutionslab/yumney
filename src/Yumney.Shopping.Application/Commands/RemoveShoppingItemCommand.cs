using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shared.CQRS;
using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingList;

namespace SmartSolutionsLab.Yumney.Shopping.Application.Commands;

public sealed record RemoveShoppingItemCommand(
	ItemName ItemName,
	Quantity? Quantity,
	RemovalReason? Reason) : ICommand<Result>;
