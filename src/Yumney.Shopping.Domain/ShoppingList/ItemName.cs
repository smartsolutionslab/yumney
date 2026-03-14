using SmartSolutionsLab.Yumney.Shared.Guards;

namespace SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingList;

public sealed record ItemName
{
    public const int MaxLength = 200;

    public string Value { get; }

    public ItemName(string value)
    {
        string validated = Ensure.That(value)
            .IsNotNullOrWhiteSpace()
            .HasMaxLength(MaxLength)
            .AndReturn();
        Value = validated.Trim();
    }

    public override string ToString() => Value;
}
