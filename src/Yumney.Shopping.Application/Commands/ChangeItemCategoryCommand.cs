using SmartSolutionsLab.Yumney.Shared.CQRS;
using SmartSolutionsLab.Yumney.Shared.Outcomes;
using SmartSolutionsLab.Yumney.Shared.Quantities;
using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingList;

namespace SmartSolutionsLab.Yumney.Shopping.Application.Commands;

public sealed record ChangeItemCategoryCommand(
	ShoppingListIdentifier ListIdentifier,
	ShoppingListItemIdentifier ItemId,
	IngredientCategory Category) : ICommand<Result>;
