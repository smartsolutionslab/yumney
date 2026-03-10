using System.Globalization;
using Yumney.Shared.Guards;

namespace Yumney.Recipes.Domain.Recipe;

public sealed record PreparationTime
{
    public int Value { get; }

    public PreparationTime(int value)
    {
        Value = Ensure.That(value).IsNotNegative().AndReturn();
    }

    public static PreparationTime? FromNullable(int? value) =>
        value.HasValue ? new PreparationTime(value.Value) : null;

    public override string ToString() => Value.ToString(CultureInfo.InvariantCulture);
}
