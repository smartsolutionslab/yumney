namespace Yumney.Modules.Recipes.Domain.Ingredient;

public record Unit
{
    public string Value { get; }

    public Unit(string value)
    {
        Value = value.Trim().ToLowerInvariant();
    }

    public static Unit Gram => new("g");
    public static Unit Kilogram => new("kg");
    public static Unit Milliliter => new("ml");
    public static Unit Liter => new("l");
    public static Unit Teaspoon => new("tsp");
    public static Unit Tablespoon => new("tbsp");
    public static Unit Piece => new("pc");
    public static Unit Pinch => new("pinch");
    public static Unit Cup => new("cup");

    public static implicit operator string(Unit u) => u.Value;
}
