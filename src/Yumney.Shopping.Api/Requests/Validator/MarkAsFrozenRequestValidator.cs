using FluentValidation;

namespace SmartSolutionsLab.Yumney.Shopping.Api.Requests.Validator;

public sealed class MarkAsFrozenRequestValidator : AbstractValidator<MarkAsFrozenRequest>
{
	public MarkAsFrozenRequestValidator()
	{
		RuleFor(x => x.Name).NotEmpty().WithMessage("Item name is required.");
	}
}
