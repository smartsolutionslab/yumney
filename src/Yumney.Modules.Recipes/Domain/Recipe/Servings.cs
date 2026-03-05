using Yumney.Shared.Guards;

namespace Yumney.Modules.Recipes.Domain.Recipe;

public record Servings
{
    public int Value { get; }

    public Servings(int value)
    {
        Ensure.That(value).IsPositive();
        Value = value;
    }

    public static implicit operator int(Servings s) => s.Value;

    public static implicit operator decimal(Servings s) => s.Value;
}
