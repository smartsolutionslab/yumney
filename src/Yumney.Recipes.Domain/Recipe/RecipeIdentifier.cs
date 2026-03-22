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

    public override string ToString() => Value.ToString();
}
