using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shared.Guards;

namespace SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingList;

public sealed record OwnerIdentifier : IValueObject<string>
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

    public static implicit operator string(OwnerIdentifier obj) => obj.Value;
}
