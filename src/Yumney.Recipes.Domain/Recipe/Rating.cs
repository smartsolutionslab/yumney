using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shared.Guards;

namespace SmartSolutionsLab.Yumney.Recipes.Domain.Recipe;

public sealed record Rating : IValueObject<int>
{
	public const int MinValue = 1;
	public const int MaxValue = 5;

	public int Value { get; }

	private Rating(int value)
	{
		Value = Ensure.That(value).IsInRange(MinValue, MaxValue).AndReturn();
	}

	public static Rating From(int value) => new(value);

	public static Rating? FromNullable(int? value) => value.HasValue ? new Rating(value.Value) : null;

	public static implicit operator int(Rating obj) => obj.Value;
}
