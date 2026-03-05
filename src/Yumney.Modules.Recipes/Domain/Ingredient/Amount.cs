using Yumney.Shared.Guards;

namespace Yumney.Modules.Recipes.Domain.Ingredient;

public record Amount
{
    public decimal Value { get; }

    public Amount(decimal value)
    {
        Ensure.That(value).IsNotNegative();
        Value = value;
    }

    public static implicit operator decimal(Amount a) => a.Value;
}
