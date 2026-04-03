using System.Globalization;
using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shared.Guards;

namespace SmartSolutionsLab.Yumney.Recipes.Domain.Recipe;

public sealed record Servings : IValueObject<int>
{
    public const int MinValue = 1;

    public int Value { get; }

    private Servings(int value)
    {
        Value = Ensure.That(value).IsPositive().AndReturn();
    }

    public static Servings From(int value) => new(value);

    public static Servings? FromNullable(int? value) =>
        value.HasValue ? new Servings(value.Value) : null;

    public static implicit operator int(Servings obj) => obj.Value;

    public override string ToString() => Value.ToString(CultureInfo.InvariantCulture);
}
