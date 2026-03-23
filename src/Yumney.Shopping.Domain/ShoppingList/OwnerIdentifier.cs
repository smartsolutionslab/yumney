using SmartSolutionsLab.Yumney.Shared.Guards;

namespace SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingList;

public sealed record OwnerIdentifier
{
    public const int MaxLength = 255;

    public string Value { get; }

    private OwnerIdentifier(string value)
    {
        string validated = Ensure.That(value)
            .IsNotNullOrWhiteSpace()
            .HasMaxLength(MaxLength)
            .AndReturn();
        Value = validated.Trim();
    }

    public static OwnerIdentifier From(string value) => new(value);

    public override string ToString() => Value;
}
