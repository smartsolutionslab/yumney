using FluentValidation;
using SmartSolutionsLab.Yumney.Recipes.Domain.Recipe;

namespace SmartSolutionsLab.Yumney.Recipes.Api.Requests.Validator;

public sealed class SaveRecipeIngredientRequestValidator : AbstractValidator<SaveRecipeIngredientRequest>
{
    public SaveRecipeIngredientRequestValidator()
    {
        RuleFor(i => i.Name)
            .NotEmpty()
            .MaximumLength(IngredientName.MaxLength);

        RuleFor(i => i.Amount)
            .GreaterThanOrEqualTo(0)
            .When(i => i.Amount.HasValue);

        RuleFor(i => i.Unit)
            .MaximumLength(Unit.MaxLength)
            .When(i => i.Unit is not null);
    }
}
