using SmartSolutionsLab.Yumney.Shared.Guards;

namespace SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingList;

public sealed record RecipeReference
{
    public Guid Value { get; }

    private RecipeReference(Guid value)
    {
        Value = Ensure.That(value).IsNotEmpty().AndReturn();
    }

    public static RecipeReference From(Guid value) => new(value);

    public static RecipeReference? FromNullable(Guid? value) =>
        value.HasValue ? new RecipeReference(value.Value) : null;

    public override string ToString() => Value.ToString();
}
