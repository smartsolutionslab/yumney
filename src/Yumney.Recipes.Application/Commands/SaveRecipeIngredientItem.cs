using SmartSolutionsLab.Yumney.Recipes.Domain.Recipe;

namespace SmartSolutionsLab.Yumney.Recipes.Application.Commands;

public sealed record SaveRecipeIngredientItem(
    IngredientName Name,
    Amount? Amount,
    Unit? Unit)
{
    public Ingredient ToDomain() => Ingredient.Create(Name, Amount, Unit);
}
