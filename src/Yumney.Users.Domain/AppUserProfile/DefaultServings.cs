using System.Globalization;
using SmartSolutionsLab.Yumney.Shared.Abstractions;
using SmartSolutionsLab.Yumney.Shared.Guards;

namespace SmartSolutionsLab.Yumney.Users.Domain.AppUserProfile;

public sealed record DefaultServings : IValueObject<int>
{
	public const int MinValue = 1;
	public const int MaxValue = 12;
	public const int DefaultValue = 4;

	public static readonly DefaultServings Default = new(DefaultValue);

	public int Value { get; }

	private DefaultServings(int value)
	{
		Value = Ensure.That(value).IsInRange(MinValue, MaxValue).AndReturn();
	}

	public static DefaultServings From(int value) => new(value);

	public static DefaultServings? FromNullable(int? value) => value.HasValue ? new DefaultServings(value.Value) : null;

	public static implicit operator int(DefaultServings obj) => obj.Value;

	public override string ToString() => Value.ToString(CultureInfo.InvariantCulture);
}
