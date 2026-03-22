using SmartSolutionsLab.Yumney.Shared.Guards;

namespace SmartSolutionsLab.Yumney.Recipes.Domain.Recipe;

public sealed record RecipeIdentifier
{
    public Guid Value { get; }

    public RecipeIdentifier(Guid value)
    {
        Value = Ensure.That(value).IsNotEmpty().AndReturn();
    }

    public static RecipeIdentifier New() => new(Guid.NewGuid());

    public static RecipeIdentifier From(Guid value) => new(value);

    public static RecipeIdentifier? FromNullable(Guid? value) =>
        value.HasValue ? new RecipeIdentifier(value.Value) : null;

    public override string ToString() => Value.ToString();
}
