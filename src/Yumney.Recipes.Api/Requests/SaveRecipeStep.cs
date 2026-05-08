using SmartSolutionsLab.Yumney.Recipes.Application.Commands;
using SmartSolutionsLab.Yumney.Recipes.Domain.Recipe;

namespace SmartSolutionsLab.Yumney.Recipes.Api.Requests;

public sealed record SaveRecipeStep(int Number, string Description)
{
	public SaveRecipeStepItem ToCommandItem() => new(
		StepNumber.From(Number),
		StepDescription.From(Description));
}
