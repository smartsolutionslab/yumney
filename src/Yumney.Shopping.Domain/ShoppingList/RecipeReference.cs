using SmartSolutionsLab.Yumney.Shared.Guards;

namespace SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingList;

public sealed record RecipeReference
{
    public Guid Value { get; }

    public RecipeReference(Guid value)
    {
        Value = Ensure.That(value).IsNotEmpty().AndReturn();
    }

    public static RecipeReference? FromNullable(Guid? value)
    {
        return value.HasValue ? new RecipeReference(value.Value) : null;
    }

    public override string ToString() => Value.ToString();
}
