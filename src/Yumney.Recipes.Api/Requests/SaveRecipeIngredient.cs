using SmartSolutionsLab.Yumney.Recipes.Application.Commands;
using SmartSolutionsLab.Yumney.Recipes.Domain.Recipe;

namespace SmartSolutionsLab.Yumney.Recipes.Api.Requests;

public sealed record SaveRecipeIngredient(string Name, decimal? Amount, string? Unit)
{
	public SaveRecipeIngredientItem ToCommandItem() => new(
		IngredientName.From(Name),
		Quantity.FromNullable(
			Domain.Recipe.Amount.FromNullable(Amount),
			Domain.Recipe.Unit.FromNullable(Unit)));
}
