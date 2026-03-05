using Yumney.Modules.Recipes.Domain.Ingredient;

namespace Yumney.Modules.Recipes.Domain.Recipe;

public class RecipeIngredient
{
    public RecipeIngredient(
        Guid id,
        RecipeId recipeId,
        IngredientName name,
        Quantity quantity,
        int sortOrder,
        IngredientCategory? category = null)
    {
        Id = id;
        RecipeId = recipeId;
        Name = name;
        Quantity = quantity;
        SortOrder = sortOrder;
        Category = category;
    }

    public Guid Id { get; private set; }

    public RecipeId RecipeId { get; private set; }

    public IngredientName Name { get; private set; }

    public Quantity Quantity { get; private set; }

    public int SortOrder { get; private set; }

    public IngredientCategory? Category { get; private set; }
}
