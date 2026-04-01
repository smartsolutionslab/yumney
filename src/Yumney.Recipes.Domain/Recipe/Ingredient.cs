using SmartSolutionsLab.Yumney.Shared.Common;

namespace SmartSolutionsLab.Yumney.Recipes.Domain.Recipe;

public sealed class Ingredient : Entity<IngredientIdentifier>
{
    public IngredientName Name { get; private set; } = default!;

    public Quantity? Quantity { get; private set; }

    private Ingredient()
    {
    }

    public static Ingredient Create(IngredientName name, Quantity? quantity)
    {
        return new Ingredient
        {
            Id = IngredientIdentifier.New(),
            Name = name,
            Quantity = quantity,
        };
    }
}
