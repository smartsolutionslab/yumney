using Yumney.Modules.Recipes.Domain.Recipe;
using Yumney.Shared.Guards;

namespace Yumney.Modules.Recipes.Domain.Ingredient;

public record Quantity
{
    public Amount Amount { get; }

    public Unit Unit { get; }

    public Quantity(Amount amount, Unit unit)
    {
        Ensure.That(amount).IsNotNull();
        Ensure.That(unit).IsNotNull();
        Amount = amount;
        Unit = unit;
    }

    public Quantity ScaledTo(Servings original, Servings desired)
    {
        decimal factor = (decimal)desired / (decimal)original;
        var scaledAmount = new Amount(Math.Round(Amount.Value * factor, 2));
        return new Quantity(scaledAmount, Unit);
    }

    public void Deconstruct(out Amount amount, out Unit unit)
    {
        amount = Amount;
        unit = Unit;
    }

    public override string ToString() => $"{Amount.Value} {Unit.Value}";
}
