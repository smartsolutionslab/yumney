using FluentValidation;

namespace SmartSolutionsLab.Yumney.Recipes.Api.Requests.Validator;

public sealed class ImportFromTextValidator : AbstractValidator<ImportFromText>
{
	public ImportFromTextValidator()
	{
		RuleFor(request => request.Text).NotEmpty().WithMessage("Text cannot be empty.");
	}
}
