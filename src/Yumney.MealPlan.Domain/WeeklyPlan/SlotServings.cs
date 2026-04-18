using System.Globalization;
using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shared.Guards;

namespace SmartSolutionsLab.Yumney.MealPlan.Domain.WeeklyPlan;

public sealed record SlotServings : IValueObject<int>
{
	public int Value { get; }

	private SlotServings(int value)
	{
		Value = Ensure.That(value).IsPositive().AndReturn();
	}

	public static SlotServings From(int value) => new(value);

	public static implicit operator int(SlotServings obj) => obj.Value;

	public override string ToString() => Value.ToString(CultureInfo.InvariantCulture);
}
