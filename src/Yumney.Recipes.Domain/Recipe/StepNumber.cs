using System.Globalization;
using Yumney.Shared.Guards;

namespace Yumney.Recipes.Domain.Recipe;

public sealed record StepNumber
{
    public int Value { get; }

    public StepNumber(int value)
    {
        Value = Ensure.That(value).IsPositive().AndReturn();
    }

    public override string ToString() => Value.ToString(CultureInfo.InvariantCulture);
}
