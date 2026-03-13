using System.Globalization;
using SmartSolutionsLab.Yumney.Shared.Guards;

namespace SmartSolutionsLab.Yumney.Recipes.Domain.Recipe;

public sealed record StepNumber
{
    public int Value { get; }

    public StepNumber(int value)
    {
        Value = Ensure.That(value).IsPositive().AndReturn();
    }

    public override string ToString() => Value.ToString(CultureInfo.InvariantCulture);
}
