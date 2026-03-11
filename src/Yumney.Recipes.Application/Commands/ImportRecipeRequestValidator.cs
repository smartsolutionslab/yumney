using FluentValidation;
using SmartSolutionsLab.Yumney.Recipes.Domain.Recipe;

namespace SmartSolutionsLab.Yumney.Recipes.Application.Commands;

public sealed class ImportRecipeRequestValidator : AbstractValidator<ImportRecipeRequest>
{
    public ImportRecipeRequestValidator()
    {
        RuleFor(x => x.Url)
            .NotEmpty()
            .MaximumLength(RecipeUrl.MaxLength)
            .Must(url => Uri.TryCreate(url, UriKind.Absolute, out var uri)
                && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
            .WithMessage("A valid HTTP or HTTPS URL is required.");
    }
}
