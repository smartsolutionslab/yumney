using System.Globalization;
using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shared.Guards;

namespace SmartSolutionsLab.Yumney.Recipes.Domain.Recipe;

public sealed record CookingTime : IValueObject
{
    public int Value { get; }

    private CookingTime(int value)
    {
        Value = Ensure.That(value).IsNotNegative().AndReturn();
    }

    public static CookingTime From(int value) => new(value);

    public static CookingTime? FromNullable(int? value) =>
        value.HasValue ? new CookingTime(value.Value) : null;

    public static implicit operator int(CookingTime obj) => obj.Value;

    public override string ToString() => Value.ToString(CultureInfo.InvariantCulture);
}
