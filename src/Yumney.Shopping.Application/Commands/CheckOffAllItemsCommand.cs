using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shared.CQRS;
using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingList;

namespace SmartSolutionsLab.Yumney.Shopping.Application.Commands;

public sealed record CheckOffAllItemsCommand(
	ShoppingListIdentifier ListIdentifier,
	bool IsChecked) : ICommand<Result>;
