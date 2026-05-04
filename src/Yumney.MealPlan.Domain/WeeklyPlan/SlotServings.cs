using System.Globalization;
using SmartSolutionsLab.Yumney.Shared.Abstractions;
using SmartSolutionsLab.Yumney.Shared.Guards;

namespace SmartSolutionsLab.Yumney.MealPlan.Domain.WeeklyPlan;

public sealed record SlotServings : IValueObject<int>
{
	public const int DefaultValue = 4;

	public int Value { get; }

	private SlotServings(int value)
	{
		Value = Ensure.That(value).IsPositive().AndReturn();
	}

	public static SlotServings From(int value) => new(value);

	public static SlotServings Default() => new(DefaultValue);

	public static implicit operator int(SlotServings obj) => obj.Value;

	public override string ToString() => Value.ToString(CultureInfo.InvariantCulture);
}
