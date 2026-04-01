using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shared.Guards;

namespace SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingList;

public sealed record Unit : IValueObject
{
    public const int MaxLength = 50;

    public string Value { get; }

    private Unit(string value)
    {
        string validated = Ensure.That(value)
            .IsNotNullOrWhiteSpace()
            .HasMaxLength(MaxLength)
            .AndReturn();
        Value = validated.Trim();
    }

    public static Unit From(string value) => new(value);

    public static Unit? FromNullable(string? value) => value.HasValue() ? new Unit(value!) : null;

    public static explicit operator string(Unit obj) => obj.Value;

    public override string ToString() => Value;
}
