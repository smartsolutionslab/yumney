using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shared.Guards;

namespace SmartSolutionsLab.Yumney.MealPlan.Domain.WeeklyPlan;

public sealed record FreetextLabel : IValueObject<string>
{
	public string Value { get; }

	private FreetextLabel(string value)
	{
		string validated = Ensure.That(value).IsNotNullOrWhiteSpace().HasMaxLength(200).AndReturn();
		Value = validated.Trim();
	}

	public static FreetextLabel From(string value) => new(value);

	public static implicit operator string(FreetextLabel obj) => obj.Value;

	public override string ToString() => Value;
}
