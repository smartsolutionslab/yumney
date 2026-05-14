using FluentValidation;

namespace SmartSolutionsLab.Yumney.Shopping.Api.Requests.Validator;

public sealed class MarkAsFrozenValidator : AbstractValidator<MarkAsFrozen>
{
	public MarkAsFrozenValidator()
	{
		RuleFor(request => request.Name).NotEmpty().WithMessage("Item name is required.");
	}
}
