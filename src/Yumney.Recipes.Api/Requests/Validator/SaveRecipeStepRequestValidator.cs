using FluentValidation;
using SmartSolutionsLab.Yumney.Recipes.Domain.Recipe;

namespace SmartSolutionsLab.Yumney.Recipes.Api.Requests.Validator;

public sealed class SaveRecipeStepRequestValidator : AbstractValidator<SaveRecipeStepRequest>
{
    public SaveRecipeStepRequestValidator()
    {
        RuleFor(s => s.Number)
            .GreaterThan(0);

        RuleFor(s => s.Description)
            .NotEmpty()
            .MaximumLength(StepDescription.MaxLength);
    }
}
