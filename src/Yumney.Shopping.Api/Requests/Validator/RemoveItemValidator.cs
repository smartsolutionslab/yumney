using FluentValidation;

namespace SmartSolutionsLab.Yumney.Shopping.Api.Requests.Validator;

public sealed class RemoveItemValidator : AbstractValidator<RemoveItem>
{
	public RemoveItemValidator()
	{
		RuleFor(request => request.Name).NotEmpty().WithMessage("Item name is required.");
	}
}
