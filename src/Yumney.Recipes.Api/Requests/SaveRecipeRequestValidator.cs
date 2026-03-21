using FluentValidation;
using SmartSolutionsLab.Yumney.Recipes.Domain.Recipe;
using SmartSolutionsLab.Yumney.Shared.CQRS;

namespace SmartSolutionsLab.Yumney.Recipes.Api.Requests;

public sealed class SaveRecipeRequestValidator : AbstractValidator<SaveRecipeRequest>
{
    public SaveRecipeRequestValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty()
            .MaximumLength(RecipeTitle.MaxLength);

        RuleFor(x => x.SourceUrl!)
            .MustBeValidHttpUrl(RecipeUrl.MaxLength)
            .When(x => !string.IsNullOrWhiteSpace(x.SourceUrl));

        RuleFor(x => x.Description)
            .MaximumLength(RecipeDescription.MaxLength)
            .When(x => x.Description is not null);

        RuleFor(x => x.ImageUrl!)
            .MustBeValidHttpUrl(ImageUrl.MaxLength)
            .When(x => !string.IsNullOrWhiteSpace(x.ImageUrl));

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

        RuleForEach(x => x.Ingredients).ChildRules(ingredient =>
        {
            ingredient.RuleFor(i => i.Name)
                .NotEmpty()
                .MaximumLength(IngredientName.MaxLength);

            ingredient.RuleFor(i => i.Amount)
                .GreaterThanOrEqualTo(0)
                .When(i => i.Amount.HasValue);

            ingredient.RuleFor(i => i.Unit)
                .MaximumLength(Unit.MaxLength)
                .When(i => i.Unit is not null);
        });

        RuleFor(x => x.Steps)
            .NotEmpty()
            .WithMessage("At least one step is required.");

        RuleForEach(x => x.Steps).ChildRules(step =>
        {
            step.RuleFor(s => s.Number)
                .GreaterThan(0);

            step.RuleFor(s => s.Description)
                .NotEmpty()
                .MaximumLength(StepDescription.MaxLength);
        });
    }
}
