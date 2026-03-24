using SmartSolutionsLab.Yumney.Shared.Guards;

namespace SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingList;

public sealed record ShoppingListTitle
{
    public const int MaxLength = 200;

    public string Value { get; }

    public ShoppingListTitle(string value)
    {
        string validated = Ensure.That(value)
            .IsNotNullOrWhiteSpace()
            .HasMaxLength(MaxLength)
            .AndReturn();
        Value = validated.Trim();
    }

    public static ShoppingListTitle From(string value) => new(value);

    public override string ToString() => Value;
}
