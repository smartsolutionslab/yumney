using System.Globalization;
using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shared.Guards;

namespace SmartSolutionsLab.Yumney.Recipes.Domain.Recipe;

public sealed record StepNumber : IValueObject<int>
{
    public int Value { get; }

    private StepNumber(int value)
    {
        Value = Ensure.That(value).IsPositive().AndReturn();
    }

    public static StepNumber From(int value) => new(value);

    public static implicit operator int(StepNumber obj) => obj.Value;

    public override string ToString() => Value.ToString(CultureInfo.InvariantCulture);
}
