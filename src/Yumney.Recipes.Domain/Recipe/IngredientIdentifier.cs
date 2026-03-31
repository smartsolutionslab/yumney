using SmartSolutionsLab.Yumney.Shared.Common;

namespace SmartSolutionsLab.Yumney.Recipes.Domain.Recipe;

public sealed record IngredientIdentifier : GuidIdentifier
{
    private IngredientIdentifier(Guid value)
        : base(value)
    {
    }

    public static IngredientIdentifier New() => new(Guid.NewGuid());

    public static IngredientIdentifier From(Guid value) => new(value);
}
