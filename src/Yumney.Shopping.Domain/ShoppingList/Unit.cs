using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shared.Guards;

namespace SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingList;

public sealed record Unit : IValueObject
{
    public const int MaxLength = 50;

    public string Value { get; }

    private Unit(string value)
    {
        string validated = Ensure.That(value)
            .IsNotNullOrWhiteSpace()
            .HasMaxLength(MaxLength)
            .AndReturn();
        Value = validated.Trim();
    }

    public static readonly Unit Grams = new("g");
    public static readonly Unit Kilograms = new("kg");
    public static readonly Unit Milliliters = new("ml");
    public static readonly Unit Liters = new("l");
    public static readonly Unit Teaspoon = new("tsp");
    public static readonly Unit Tablespoon = new("tbsp");
    public static readonly Unit Cups = new("cups");
    public static readonly Unit Pieces = new("pcs");

    public static Unit From(string value) => new(value);

    public static Unit? FromNullable(string? value) => value.HasValue() ? new Unit(value!) : null;

    public static implicit operator string(Unit obj) => obj.Value;
}
