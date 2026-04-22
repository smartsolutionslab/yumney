using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shared.Guards;

namespace SmartSolutionsLab.Yumney.MealPlan.Domain.WeeklyPlan;

public sealed record SlotRecipeIdentifier : IValueObject
{
	public Guid Value { get; }

	private SlotRecipeIdentifier(Guid value)
	{
		Value = Ensure.That(value).IsNotEmpty().AndReturn();
	}

	public static SlotRecipeIdentifier From(Guid value) => new(value);

	public static SlotRecipeIdentifier New() => new(Guid.CreateVersion7());

	public override string ToString() => Value.ToString();
}
