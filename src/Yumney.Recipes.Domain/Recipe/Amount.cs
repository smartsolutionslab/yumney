using System.Globalization;
using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shared.Guards;

namespace SmartSolutionsLab.Yumney.Recipes.Domain.Recipe;

public sealed record Amount : IValueObject<decimal>
{
    public decimal Value { get; }

    private Amount(decimal value)
    {
        Value = Ensure.That(value).IsNotNegative().AndReturn();
    }

    public static Amount From(decimal value) => new(value);

    public static Amount? FromNullable(decimal? value) =>
        value.HasValue ? new Amount(value.Value) : null;

    public static implicit operator decimal(Amount obj) => obj.Value;

    public override string ToString() => Value.ToString(CultureInfo.InvariantCulture);
}
