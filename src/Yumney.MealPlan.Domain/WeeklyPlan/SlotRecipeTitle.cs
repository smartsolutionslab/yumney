using SmartSolutionsLab.Yumney.Shared.Abstractions;
using SmartSolutionsLab.Yumney.Shared.Guards;

namespace SmartSolutionsLab.Yumney.MealPlan.Domain.WeeklyPlan;

public sealed record SlotRecipeTitle : IValueObject<string>
{
	public const int MaxLength = 200;

	public string Value { get; }

	private SlotRecipeTitle(string value)
	{
		string validated = Ensure.That(value)
			.IsNotNullOrWhiteSpace()
			.HasMaxLength(MaxLength)
			.AndReturn();
		Value = validated.Trim();
	}

	public static SlotRecipeTitle From(string value) => new(value);

	public static implicit operator string(SlotRecipeTitle title) => title.Value;
}
