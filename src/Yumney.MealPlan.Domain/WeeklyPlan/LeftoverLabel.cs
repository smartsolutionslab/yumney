using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shared.Guards;

namespace SmartSolutionsLab.Yumney.MealPlan.Domain.WeeklyPlan;

public sealed record LeftoverLabel : IValueObject<string>
{
	public string Value { get; }

	private LeftoverLabel(string value)
	{
		string validated = Ensure.That(value).IsNotNullOrWhiteSpace().HasMaxLength(200).AndReturn();
		Value = validated.Trim();
	}

	public static LeftoverLabel From(string value) => new(value);

	public static LeftoverLabel ForRecipe(string sourceRecipeTitle) =>
		new($"Leftovers: {sourceRecipeTitle}");

	public static implicit operator string(LeftoverLabel obj) => obj.Value;

	public override string ToString() => Value;
}
