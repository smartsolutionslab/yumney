using System.Globalization;
using SmartSolutionsLab.Yumney.Shared.Guards;

namespace SmartSolutionsLab.Yumney.Recipes.Domain.Recipe;

public sealed record Servings
{
    public int Value { get; }

    public Servings(int value)
    {
        Value = Ensure.That(value).IsPositive().AndReturn();
    }

    public static Servings? FromNullable(int? value) =>
        value.HasValue ? new Servings(value.Value) : null;

    public override string ToString() => Value.ToString(CultureInfo.InvariantCulture);
}
