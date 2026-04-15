using FluentValidation;

namespace SmartSolutionsLab.Yumney.Shopping.Api.Requests;

public sealed class RemoveItemRequestValidator : AbstractValidator<RemoveItemRequest>
{
    public RemoveItemRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().WithMessage("Item name is required.");
    }
}
