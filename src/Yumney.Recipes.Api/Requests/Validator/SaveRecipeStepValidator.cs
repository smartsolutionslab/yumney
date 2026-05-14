using FluentValidation;
using SmartSolutionsLab.Yumney.Recipes.Domain.Recipe;

namespace SmartSolutionsLab.Yumney.Recipes.Api.Requests.Validator;

public sealed class SaveRecipeStepValidator : AbstractValidator<SaveRecipeStep>
{
	public SaveRecipeStepValidator()
	{
		RuleFor(step => step.Number)
			.GreaterThan(0);

		RuleFor(step => step.Description)
			.NotEmpty()
			.MaximumLength(StepDescription.MaxLength);
	}
}
