using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shared.Guards;

namespace SmartSolutionsLab.Yumney.MealPlan.Domain.WeeklyPlan;

public sealed record WeeklyPlanIdentifier : IValueObject
{
	public Guid Value { get; }

	private WeeklyPlanIdentifier(Guid value)
	{
		Value = Ensure.That(value).IsNotEmpty().AndReturn();
	}

	public static WeeklyPlanIdentifier New() => new(Guid.CreateVersion7());

	public static WeeklyPlanIdentifier From(Guid value) => new(value);

	public override string ToString() => Value.ToString();
}
