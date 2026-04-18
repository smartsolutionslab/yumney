using SmartSolutionsLab.Yumney.Recipes.Domain.Recipe;

namespace SmartSolutionsLab.Yumney.Recipes.Application.Commands;

public sealed record SaveRecipeStepItem(StepNumber Number, StepDescription Description)
{
	public Step ToDomain() => Step.Create(Number, Description);
}
