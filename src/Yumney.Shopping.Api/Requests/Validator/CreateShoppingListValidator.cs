using FluentValidation;
using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingList;

namespace SmartSolutionsLab.Yumney.Shopping.Api.Requests.Validator;

public sealed class CreateShoppingListValidator : AbstractValidator<CreateShoppingList>
{
	public CreateShoppingListValidator()
	{
		RuleFor(request => request.Title)
			.NotEmpty()
			.MaximumLength(ShoppingListTitle.MaxLength);

		RuleFor(request => request.Items)
			.NotEmpty()
			.WithMessage("At least one item is required.");

		RuleForEach(request => request.Items).ChildRules(itemRule =>
		{
			itemRule.RuleFor(item => item.Name)
				.NotEmpty()
				.MaximumLength(ItemName.MaxLength);

			itemRule.RuleFor(item => item.Amount)
				.GreaterThanOrEqualTo(0)
				.When(item => item.Amount.HasValue);

			itemRule.RuleFor(item => item.Unit)
				.MaximumLength(Unit.MaxLength)
				.When(item => item.Unit is not null);
		});
	}
}
