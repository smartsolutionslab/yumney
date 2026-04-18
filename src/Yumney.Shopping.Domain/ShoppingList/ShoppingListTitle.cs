using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shared.Guards;

namespace SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingList;

public sealed record ShoppingListTitle : IValueObject<string>
{
	public const int MaxLength = 200;

	public string Value { get; }

	private ShoppingListTitle(string value)
	{
		string validated = Ensure.That(value)
			.IsNotNullOrWhiteSpace()
			.HasMaxLength(MaxLength)
			.AndReturn();
		Value = validated.Trim();
	}

	public static ShoppingListTitle From(string value) => new(value);

	public static implicit operator string(ShoppingListTitle obj) => obj.Value;
}
