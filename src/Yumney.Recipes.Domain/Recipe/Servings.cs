using System.Globalization;
using Yumney.Shared.Guards;

namespace Yumney.Recipes.Domain.Recipe;

public sealed record Servings
{
    public int Value { get; }

    public Servings(int value)
    {
        Value = Ensure.That(value).IsPositive().AndReturn();
    }

    public override string ToString() => Value.ToString(CultureInfo.InvariantCulture);
}
