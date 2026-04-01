using SmartSolutionsLab.Yumney.Recipes.Domain.Recipe;

namespace SmartSolutionsLab.Yumney.Recipes.Application.DTOs;

public static class RecipeMappingExtensions
{
    extension(Recipe recipe)
    {
        public RecipeDetailDto ToDetailDto()
        {
            return new RecipeDetailDto(
                recipe.Id.Value,
                recipe.Title.Value,
                recipe.Description?.Value,
                recipe.Servings?.Value,
                recipe.PreparationTime?.Value,
                recipe.CookingTime?.Value,
                recipe.Difficulty?.Value,
                recipe.ImageUrl?.Value,
                recipe.Language?.Value,
                recipe.SourceUrl?.Value,
                recipe.CreatedAt,
                recipe.Ingredients.Select(i => i.ToDto()).ToList(),
                recipe.Steps.Select(s => s.ToDto()).ToList(),
                recipe.Tags.Select(t => t.Value).ToList());
        }

        public RecipeListItemDto ToListItemDto()
        {
            return new RecipeListItemDto(
                recipe.Id.Value,
                recipe.Title.Value,
                recipe.Description?.Value,
                recipe.Servings?.Value,
                recipe.PreparationTime?.Value,
                recipe.CookingTime?.Value,
                recipe.Difficulty?.Value,
                recipe.ImageUrl?.Value,
                recipe.CreatedAt,
                recipe.Tags.Select(t => t.Value).ToList());
        }
    }

    public static RecipeIngredientDto ToDto(this Ingredient ingredient)
    {
        return new RecipeIngredientDto(
            ingredient.Name.Value,
            ingredient.Amount?.Value,
            ingredient.Unit?.Value);
    }

    public static RecipeStepDto ToDto(this Step step) => new(step.Number.Value, step.Description.Value);

    public static SavedRecipeDto ToSavedDto(this Recipe recipe) => new(recipe.Id.Value, recipe.Title.Value, recipe.CreatedAt);
}
