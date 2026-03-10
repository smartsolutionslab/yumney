using System.Globalization;
using Yumney.Shared.Guards;

namespace Yumney.Recipes.Domain.Recipe;

public sealed record CookingTime
{
    public int Value { get; }

    public CookingTime(int value)
    {
        Value = Ensure.That(value).IsNotNegative().AndReturn();
    }

    public override string ToString() => Value.ToString(CultureInfo.InvariantCulture);
}
