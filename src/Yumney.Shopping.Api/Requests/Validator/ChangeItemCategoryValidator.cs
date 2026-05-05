using FluentValidation;
using SmartSolutionsLab.Yumney.Shared.Quantities;

namespace SmartSolutionsLab.Yumney.Shopping.Api.Requests.Validator;

public sealed class ChangeItemCategoryValidator : AbstractValidator<ChangeItemCategory>
{
	public ChangeItemCategoryValidator()
	{
		RuleFor(x => x.Category)
			.NotEmpty().WithMessage("Category is required.")
			.Must(BeKnownCategory).WithMessage("Unknown category.");
	}

	private static bool BeKnownCategory(string value)
	{
		try
		{
			IngredientCategory.From(value);
			return true;
		}
		catch
		{
			return false;
		}
	}
}
