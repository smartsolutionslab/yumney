using SmartSolutionsLab.Yumney.Shared.Outcomes;

namespace SmartSolutionsLab.Yumney.Shopping.Application.Commands;

public static class CreateShoppingListFromRecipesErrors
{
	public static readonly ApiError NoRecipesProvided = new(
		"SHOPPING_LIST_FROM_RECIPES_EMPTY",
		"At least one recipe must be provided.",
		400);

	public static readonly ApiError NoIngredientsResolved = new(
		"SHOPPING_LIST_FROM_RECIPES_NO_INGREDIENTS",
		"None of the supplied recipes returned ingredients.",
		404);
}
