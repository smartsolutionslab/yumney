using FluentValidation;
using SmartSolutionsLab.Yumney.Recipes.Domain.Recipe;
using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shared.CQRS;

namespace SmartSolutionsLab.Yumney.Recipes.Api.Requests.Validator;

public sealed class UpdateRecipeRequestValidator : AbstractValidator<UpdateRecipeRequest>
{
    public UpdateRecipeRequestValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty()
            .MaximumLength(RecipeTitle.MaxLength);

        RuleFor(x => x.Description)
            .MaximumLength(RecipeDescription.MaxLength)
            .When(x => x.Description is not null);

        RuleFor(x => x.ImageUrl!)
            .MustBeValidHttpUrl(ImageUrl.MaxLength)
            .When(x => x.ImageUrl.HasValue());

        RuleFor(x => x.Difficulty)
            .NotEmpty()
            .MaximumLength(Difficulty.MaxLength)
            .When(x => x.Difficulty is not null);

        RuleFor(x => x.Servings)
            .GreaterThan(0)
            .When(x => x.Servings.HasValue);

        RuleFor(x => x.PrepTimeMinutes)
            .GreaterThanOrEqualTo(0)
            .When(x => x.PrepTimeMinutes.HasValue);

        RuleFor(x => x.CookTimeMinutes)
            .GreaterThanOrEqualTo(0)
            .When(x => x.CookTimeMinutes.HasValue);

        RuleFor(x => x.Ingredients)
            .NotEmpty()
            .WithMessage("At least one ingredient is required.");
        RuleForEach(x => x.Ingredients).SetValidator(new SaveRecipeIngredientRequestValidator());

        RuleFor(x => x.Steps)
            .NotEmpty()
            .WithMessage("At least one step is required.");
        RuleForEach(x => x.Steps).SetValidator(new SaveRecipeStepRequestValidator());
    }
}
