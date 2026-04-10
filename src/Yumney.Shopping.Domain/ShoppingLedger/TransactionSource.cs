using SmartSolutionsLab.Yumney.Shared.Common;

namespace SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingLedger;

public sealed record TransactionSource : IValueObject<string>
{
    public const int MaxLength = 500;
    public const string ManualValue = "manual";

    public static readonly TransactionSource Manual = new(ManualValue);

    public string Value { get; }

    private TransactionSource(string value)
    {
        Value = value;
    }

    public static TransactionSource FromRecipe(Guid recipeIdentifier, string? mealSlot = null)
    {
        var source = $"recipe:{recipeIdentifier}";
        if (mealSlot is not null)
            source += $"|slot:{mealSlot}";
        return new TransactionSource(source);
    }

    public static TransactionSource From(string value) => new(value);

    public bool IsManual => Value == ManualValue;

    public static implicit operator string(TransactionSource obj) => obj.Value;
}
