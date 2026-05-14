using FluentValidation;
using SmartSolutionsLab.Yumney.Recipes.Domain.Recipe;
using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shared.CQRS;

namespace SmartSolutionsLab.Yumney.Recipes.Api.Requests.Validator;

public sealed class SaveRecipeValidator : AbstractValidator<SaveRecipe>
{
	public SaveRecipeValidator()
	{
		RuleFor(request => request.Title)
			.NotEmpty()
			.MaximumLength(RecipeTitle.MaxLength);

		RuleFor(request => request.SourceUrl!)
			.MustBeValidHttpUrl(RecipeUrl.MaxLength)
			.When(request => request.SourceUrl.HasValue());

		ConfigureOptionalFieldRules();

		RuleFor(request => request.Ingredients)
			.NotEmpty()
			.WithMessage("At least one ingredient is required.");
		RuleForEach(request => request.Ingredients).SetValidator(new SaveRecipeIngredientValidator());

		RuleFor(request => request.Steps)
			.NotEmpty()
			.WithMessage("At least one step is required.");
		RuleForEach(request => request.Steps).SetValidator(new SaveRecipeStepValidator());
	}

	private void ConfigureOptionalFieldRules()
	{
		RuleFor(request => request.Description)
			.MaximumLength(RecipeDescription.MaxLength)
			.When(request => request.Description is not null);

		RuleFor(request => request.ImageUrl!)
			.MustBeValidHttpUrl(ImageUrl.MaxLength)
			.When(request => request.ImageUrl.HasValue());

		RuleFor(request => request.Difficulty)
			.NotEmpty()
			.MaximumLength(Difficulty.MaxLength)
			.When(request => request.Difficulty is not null);

		RuleFor(request => request.Servings)
			.GreaterThan(0)
			.When(request => request.Servings.HasValue);

		RuleFor(request => request.PrepTimeMinutes)
			.GreaterThanOrEqualTo(0)
			.When(request => request.PrepTimeMinutes.HasValue);

		RuleFor(request => request.CookTimeMinutes)
			.GreaterThanOrEqualTo(0)
			.When(request => request.CookTimeMinutes.HasValue);
	}
}
