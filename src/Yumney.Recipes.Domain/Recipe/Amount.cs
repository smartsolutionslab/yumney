using System.Globalization;
using Yumney.Shared.Guards;

namespace Yumney.Recipes.Domain.Recipe;

public sealed record Amount
{
    public decimal Value { get; }

    public Amount(decimal value)
    {
        Value = Ensure.That(value).IsNotNegative().AndReturn();
    }

    public static Amount? FromNullable(decimal? value) =>
        value.HasValue ? new Amount(value.Value) : null;

    public override string ToString() => Value.ToString(CultureInfo.InvariantCulture);
}
