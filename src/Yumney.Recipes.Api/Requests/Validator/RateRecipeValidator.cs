using FluentValidation;
using SmartSolutionsLab.Yumney.Recipes.Domain.Recipe;

namespace SmartSolutionsLab.Yumney.Recipes.Api.Requests.Validator;

public sealed class RateRecipeValidator : AbstractValidator<RateRecipe>
{
	public RateRecipeValidator()
	{
		RuleFor(request => request.Rating)
			.InclusiveBetween(Rating.MinValue, Rating.MaxValue)
			.WithMessage($"Rating must be between {Rating.MinValue} and {Rating.MaxValue}.");
	}
}
