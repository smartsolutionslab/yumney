using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shared.Guards;

namespace SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingList;

public sealed record RecipeReference : IValueObject<Guid>
{
    public Guid Value { get; }

    private RecipeReference(Guid value)
    {
        Value = Ensure.That(value).IsNotEmpty().AndReturn();
    }

    public static RecipeReference New() => new(Guid.NewGuid());

    public static RecipeReference From(Guid value) => new(value);

    public static RecipeReference? FromNullable(Guid? value) =>
        value.HasValue ? new RecipeReference(value.Value) : null;

    public static implicit operator Guid(RecipeReference obj) => obj.Value;

    public override string ToString() => Value.ToString();
}
