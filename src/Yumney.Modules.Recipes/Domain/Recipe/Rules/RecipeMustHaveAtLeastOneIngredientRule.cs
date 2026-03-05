using Yumney.Shared.Common;

namespace Yumney.Modules.Recipes.Domain.Recipe.Rules;

public class RecipeMustHaveAtLeastOneIngredientRule : IBusinessRule
{
    private readonly IReadOnlyList<RecipeIngredient> _ingredients;

    public RecipeMustHaveAtLeastOneIngredientRule(IReadOnlyList<RecipeIngredient> ingredients)
    {
        _ingredients = ingredients;
    }

    public string Message => "A recipe must have at least one ingredient.";

    public bool IsBroken() => _ingredients.Count == 0;
}
