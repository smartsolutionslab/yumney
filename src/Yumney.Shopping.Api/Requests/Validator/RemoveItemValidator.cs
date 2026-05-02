using FluentValidation;

namespace SmartSolutionsLab.Yumney.Shopping.Api.Requests.Validator;

public sealed class RemoveItemValidator : AbstractValidator<RemoveItem>
{
	public RemoveItemValidator()
	{
		RuleFor(x => x.Name).NotEmpty().WithMessage("Item name is required.");
	}
}
