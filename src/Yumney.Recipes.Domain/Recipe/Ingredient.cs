using SmartSolutionsLab.Yumney.Shared.Common;

namespace SmartSolutionsLab.Yumney.Recipes.Domain.Recipe;

public sealed class Ingredient : Entity<IngredientIdentifier>
{
    public IngredientName Name { get; private set; } = default!;

    public Amount? Amount { get; private set; }

    public Unit? Unit { get; private set; }

    private Ingredient()
    {
    }

    public static Ingredient Create(IngredientName name, Amount? amount, Unit? unit)
    {
        return new Ingredient
        {
            Id = IngredientIdentifier.New(),
            Name = name,
            Amount = amount,
            Unit = unit,
        };
    }
}
