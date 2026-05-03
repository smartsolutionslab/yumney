using SmartSolutionsLab.Yumney.Shopping.Application.Commands;
using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingList;

namespace SmartSolutionsLab.Yumney.Shopping.Api.Requests;

public sealed record CreateFromRecipes(string Title, List<RecipeForList> Recipes)
{
	public (ShoppingListTitle Title, IReadOnlyList<RecipeSelection> Recipes) ToValueObjects() =>
	(
		ShoppingListTitle.From(Title),
		Recipes.Select(recipe => new RecipeSelection(
			RecipeReference.From(recipe.RecipeIdentifier),
			Servings.FromNullable(recipe.Servings)))
			.ToList());
}
