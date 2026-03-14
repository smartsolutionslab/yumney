using FluentValidation;
using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingList;

namespace SmartSolutionsLab.Yumney.Shopping.Application.Commands;

public sealed class CreateShoppingListRequestValidator : AbstractValidator<CreateShoppingListRequest>
{
    public CreateShoppingListRequestValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty()
            .MaximumLength(ShoppingListTitle.MaxLength);

        RuleFor(x => x.Items)
            .NotEmpty()
            .WithMessage("At least one item is required.");

        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.Name)
                .NotEmpty()
                .MaximumLength(ItemName.MaxLength);

            item.RuleFor(i => i.Amount)
                .GreaterThanOrEqualTo(0)
                .When(i => i.Amount.HasValue);

            item.RuleFor(i => i.Unit)
                .MaximumLength(Unit.MaxLength)
                .When(i => i.Unit is not null);
        });
    }
}
