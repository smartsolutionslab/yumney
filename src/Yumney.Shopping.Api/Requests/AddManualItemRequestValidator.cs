using FluentValidation;

namespace SmartSolutionsLab.Yumney.Shopping.Api.Requests;

public sealed class AddManualItemRequestValidator : AbstractValidator<AddManualItemRequest>
{
    public AddManualItemRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().WithMessage("Item name is required.");
    }
}
