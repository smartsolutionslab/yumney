using FluentValidation;
using SmartSolutionsLab.Yumney.Recipes.Domain.Recipe;

namespace SmartSolutionsLab.Yumney.Recipes.Api.Requests.Validator;

public sealed class SaveRecipeIngredientValidator : AbstractValidator<SaveRecipeIngredient>
{
	public SaveRecipeIngredientValidator()
	{
		RuleFor(ingredient => ingredient.Name)
			.NotEmpty()
			.MaximumLength(IngredientName.MaxLength);

		RuleFor(ingredient => ingredient.Amount)
			.GreaterThanOrEqualTo(0)
			.When(ingredient => ingredient.Amount.HasValue);

		RuleFor(ingredient => ingredient.Unit)
			.MaximumLength(Unit.MaxLength)
			.When(ingredient => ingredient.Unit is not null);
	}
}
