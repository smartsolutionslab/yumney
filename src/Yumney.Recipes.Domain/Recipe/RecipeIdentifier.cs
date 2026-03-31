using SmartSolutionsLab.Yumney.Shared.Common;

namespace SmartSolutionsLab.Yumney.Recipes.Domain.Recipe;

public sealed record RecipeIdentifier : GuidIdentifier
{
    private RecipeIdentifier(Guid value)
        : base(value)
    {
    }

    public static RecipeIdentifier New() => new(Guid.NewGuid());

    public static RecipeIdentifier From(Guid value) => new(value);

    public static RecipeIdentifier? FromNullable(Guid? value) =>
        value.HasValue ? new RecipeIdentifier(value.Value) : null;
}
