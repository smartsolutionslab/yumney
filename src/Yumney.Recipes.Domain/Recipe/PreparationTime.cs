using System.Globalization;
using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shared.Guards;

namespace SmartSolutionsLab.Yumney.Recipes.Domain.Recipe;

public sealed record PreparationTime : IValueObject
{
    public int Value { get; }

    private PreparationTime(int value)
    {
        Value = Ensure.That(value).IsNotNegative().AndReturn();
    }

    public static PreparationTime From(int value) => new(value);

    public static PreparationTime? FromNullable(int? value) =>
        value.HasValue ? new PreparationTime(value.Value) : null;

    public static explicit operator int(PreparationTime obj) => obj.Value;

    public override string ToString() => Value.ToString(CultureInfo.InvariantCulture);
}
