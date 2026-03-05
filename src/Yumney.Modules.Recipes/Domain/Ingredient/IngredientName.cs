using Yumney.Shared.Guards;

namespace Yumney.Modules.Recipes.Domain.Ingredient;

public record IngredientName
{
    public string Value { get; }

    public IngredientName(string value)
    {
        Value = Ensure.That(value)
            .IsNotNullOrWhiteSpace()
            .HasMaxLength(200)
            .AndReturn()
            .Trim();
    }

    public static implicit operator string(IngredientName name) => name.Value;
}
