using SmartSolutionsLab.Yumney.Shared.Abstractions;
using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shared.Guards;

namespace SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingList;

public sealed record Unit : IValueObject
{
	public const int MaxLength = 50;

	// Weight
	public static readonly Unit Gram = new("g");
	public static readonly Unit Kilogram = new("kg");
	public static readonly Unit Milligram = new("mg");
	public static readonly Unit Pound = new("lb");
	public static readonly Unit Ounce = new("oz");

	// Volume
	public static readonly Unit Liter = new("l");
	public static readonly Unit Milliliter = new("ml");
	public static readonly Unit Centiliter = new("cl");
	public static readonly Unit Deciliter = new("dl");
	public static readonly Unit Cup = new("cup");
	public static readonly Unit Tablespoon = new("tbsp");
	public static readonly Unit Teaspoon = new("tsp");
	public static readonly Unit FluidOunce = new("fl oz");

	// Count & Packaging
	public static readonly Unit Piece = new("piece");
	public static readonly Unit Bunch = new("bunch");
	public static readonly Unit Can = new("can");
	public static readonly Unit Pack = new("pack");
	public static readonly Unit Bag = new("bag");
	public static readonly Unit Bottle = new("bottle");
	public static readonly Unit Slice = new("slice");
	public static readonly Unit Clove = new("clove");
	public static readonly Unit Pinch = new("pinch");
	public static readonly Unit Dash = new("dash");
	public static readonly Unit Sprig = new("sprig");

	public string Value { get; }

	private Unit(string value)
	{
		string validated = Ensure.That(value)
			.IsNotNullOrWhiteSpace()
			.HasMaxLength(MaxLength)
			.AndReturn();
		Value = validated.Trim();
	}

	public static Unit From(string value) => new(value);

	public static Unit? FromNullable(string? value) => value.HasValue() ? new Unit(value!) : null;

	public static implicit operator string(Unit obj) => obj.Value;
}
