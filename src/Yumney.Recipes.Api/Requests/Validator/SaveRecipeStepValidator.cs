using FluentValidation;
using SmartSolutionsLab.Yumney.Recipes.Domain.Recipe;

namespace SmartSolutionsLab.Yumney.Recipes.Api.Requests.Validator;

public sealed class SaveRecipeStepValidator : AbstractValidator<SaveRecipeStep>
{
	public SaveRecipeStepValidator()
	{
		RuleFor(s => s.Number)
			.GreaterThan(0);

		RuleFor(s => s.Description)
			.NotEmpty()
			.MaximumLength(StepDescription.MaxLength);
	}
}
