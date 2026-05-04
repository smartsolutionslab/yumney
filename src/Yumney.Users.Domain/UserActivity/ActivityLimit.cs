using SmartSolutionsLab.Yumney.Shared.Abstractions;
using SmartSolutionsLab.Yumney.Shared.Guards;

namespace SmartSolutionsLab.Yumney.Users.Domain.UserActivity;

public sealed record ActivityLimit : IValueObject
{
	public const int DefaultValue = 5;

	public int Value { get; }

	private ActivityLimit(int value)
	{
		Value = Ensure.That(value).IsPositive().AndReturn();
	}

	public static ActivityLimit From(int value) => new(value);

	public static ActivityLimit Default() => new(DefaultValue);

	public override string ToString() => Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
}
