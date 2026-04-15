using FluentValidation;

namespace SmartSolutionsLab.Yumney.Recipes.Api.Requests;

public sealed class ImportFromTextRequestValidator : AbstractValidator<ImportFromTextRequestDto>
{
    public ImportFromTextRequestValidator()
    {
        RuleFor(x => x.Text).NotEmpty().WithMessage("Text cannot be empty.");
    }
}
