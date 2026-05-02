using FluentValidation;

namespace SmartSolutionsLab.Yumney.Shopping.Api.Requests.Validator;

public sealed class AddManualItemValidator : AbstractValidator<AddManualItem>
{
	public AddManualItemValidator()
	{
		RuleFor(x => x.Name).NotEmpty().WithMessage("Item name is required.");
	}
}
