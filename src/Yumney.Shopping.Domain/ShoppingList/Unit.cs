using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shared.Guards;

namespace SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingList;

public sealed record Unit
{
    public const int MaxLength = 50;

    public string Value { get; }

    public Unit(string value)
    {
        string validated = Ensure.That(value)
            .IsNotNullOrWhiteSpace()
            .HasMaxLength(MaxLength)
            .AndReturn();
        Value = validated.Trim();
    }

    public static Unit? FromNullable(string? value) => value.HasValue() ? new Unit(value!) : null;

    public override string ToString() => Value;
}
