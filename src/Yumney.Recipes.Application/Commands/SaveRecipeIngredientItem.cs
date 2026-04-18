using SmartSolutionsLab.Yumney.Recipes.Domain.Recipe;

namespace SmartSolutionsLab.Yumney.Recipes.Application.Commands;

public sealed record SaveRecipeIngredientItem(
	IngredientName Name,
	Quantity? Quantity)
{
	public Ingredient ToDomain() => Ingredient.Create(Name, Quantity);
}
