using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shared.CQRS;
using SmartSolutionsLab.Yumney.Shopping.Application.DTOs;
using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingList;

namespace SmartSolutionsLab.Yumney.Shopping.Application.Commands;

public sealed record CreateShoppingListFromRecipesCommand(
	ShoppingListTitle Title,
	IReadOnlyList<RecipeSelection> Recipes) : ICommand<Result<ShoppingListDetailDto>>;
