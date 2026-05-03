using FluentValidation;
using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingList;

namespace SmartSolutionsLab.Yumney.Shopping.Api.Requests.Validator;

public sealed class CreateFromRecipesValidator : AbstractValidator<CreateFromRecipes>
{
	public CreateFromRecipesValidator()
	{
		RuleFor(request => request.Title)
			.NotEmpty()
			.MaximumLength(ShoppingListTitle.MaxLength);

		RuleFor(request => request.Recipes)
			.NotEmpty()
			.WithMessage("At least one recipe is required.");

		RuleForEach(request => request.Recipes).ChildRules(recipe =>
		{
			recipe.RuleFor(item => item.RecipeIdentifier).NotEmpty();
			recipe.RuleFor(item => item.Servings)
				.GreaterThan(0)
				.When(item => item.Servings.HasValue);
		});
	}
}
