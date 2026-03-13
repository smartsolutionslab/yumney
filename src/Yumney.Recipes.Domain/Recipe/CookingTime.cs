using System.Globalization;
using SmartSolutionsLab.Yumney.Shared.Guards;

namespace SmartSolutionsLab.Yumney.Recipes.Domain.Recipe;

public sealed record CookingTime
{
    public int Value { get; }

    public CookingTime(int value)
    {
        Value = Ensure.That(value).IsNotNegative().AndReturn();
    }

    public static CookingTime? FromNullable(int? value) =>
        value.HasValue ? new CookingTime(value.Value) : null;

    public override string ToString() => Value.ToString(CultureInfo.InvariantCulture);
}
