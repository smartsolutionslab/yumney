using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shared.Guards;

namespace SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingList;

public sealed record ItemName : IValueObject
{
    public const int MaxLength = 200;

    public string Value { get; }

    private ItemName(string value)
    {
        string validated = Ensure.That(value)
            .IsNotNullOrWhiteSpace()
            .HasMaxLength(MaxLength)
            .AndReturn();
        Value = validated.Trim();
    }

    public static ItemName From(string value) => new(value);

    public static implicit operator string(ItemName obj) => obj.Value;
}
