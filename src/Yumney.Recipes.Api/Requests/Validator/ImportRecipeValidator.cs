using FluentValidation;
using SmartSolutionsLab.Yumney.Recipes.Domain.Recipe;
using SmartSolutionsLab.Yumney.Shared.CQRS;

namespace SmartSolutionsLab.Yumney.Recipes.Api.Requests.Validator;

public sealed class ImportRecipeValidator : AbstractValidator<ImportRecipe>
{
	public ImportRecipeValidator()
	{
		RuleFor(x => x.Url)
			.NotEmpty()
			.MustBeValidHttpUrl(RecipeUrl.MaxLength);
	}
}
