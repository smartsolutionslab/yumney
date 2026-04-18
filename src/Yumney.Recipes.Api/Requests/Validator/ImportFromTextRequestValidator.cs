using FluentValidation;

namespace SmartSolutionsLab.Yumney.Recipes.Api.Requests.Validator;

public sealed class ImportFromTextRequestValidator : AbstractValidator<ImportFromTextRequestDto>
{
	public ImportFromTextRequestValidator()
	{
		RuleFor(x => x.Text).NotEmpty().WithMessage("Text cannot be empty.");
	}
}
