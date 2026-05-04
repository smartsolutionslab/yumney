using System.Globalization;
using SmartSolutionsLab.Yumney.Shared.Guards;

namespace SmartSolutionsLab.Yumney.Shared.Abstractions;

public sealed record AggregateVersion : IValueObject<int>
{
	public int Value { get; }

	private AggregateVersion(int value)
	{
		Value = Ensure.That(value).IsNotNegative().AndReturn();
	}

	public static AggregateVersion From(int value) => new(value);

	public static AggregateVersion Zero() => new(0);

	public AggregateVersion Increment() => new(Value + 1);

	public static implicit operator int(AggregateVersion version) => version.Value;

	public override string ToString() => Value.ToString(CultureInfo.InvariantCulture);
}
