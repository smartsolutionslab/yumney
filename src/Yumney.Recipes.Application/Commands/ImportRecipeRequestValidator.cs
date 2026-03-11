using FluentValidation;
using SmartSolutionsLab.Yumney.Recipes.Domain.Recipe;
using SmartSolutionsLab.Yumney.Shared.CQRS;

namespace SmartSolutionsLab.Yumney.Recipes.Application.Commands;

public sealed class ImportRecipeRequestValidator : AbstractValidator<ImportRecipeRequest>
{
    public ImportRecipeRequestValidator()
    {
        RuleFor(x => x.Url)
            .NotEmpty()
            .MustBeValidHttpUrl(RecipeUrl.MaxLength);
    }
}
