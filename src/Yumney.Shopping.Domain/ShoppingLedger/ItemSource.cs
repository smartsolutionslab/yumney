using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shared.Guards;

namespace SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingLedger;

public sealed record ItemSource : IValueObject<string>
{
	public const int MaxLength = 100;

	public string Value { get; }

	private ItemSource(string value)
	{
		string validated = Ensure.That(value)
			.IsNotNullOrWhiteSpace()
			.HasMaxLength(MaxLength)
			.AndReturn();
		Value = validated.Trim();
	}

	public static ItemSource From(string value) => new(value);

	public static ItemSource Manual => From("manual");

	public static ItemSource MealPlan => From("meal-plan");

	public static implicit operator string(ItemSource obj) => obj.Value;
}
