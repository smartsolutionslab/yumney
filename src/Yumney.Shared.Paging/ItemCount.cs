using SmartSolutionsLab.Yumney.Shared.Abstractions;
using SmartSolutionsLab.Yumney.Shared.Guards;

namespace SmartSolutionsLab.Yumney.Shared.Paging;

public sealed record ItemCount : IValueObject
{
	public int Value { get; }

	private ItemCount(int value)
	{
		Value = Ensure.That(value).IsNotNegative().AndReturn();
	}

	public static ItemCount From(int value) => new(value);

	public static implicit operator int(ItemCount obj) => obj.Value;

	public override string ToString() => Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
}
