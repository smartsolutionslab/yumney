using SmartSolutionsLab.Yumney.Recipes.Application.Commands;
using SmartSolutionsLab.Yumney.Recipes.Domain.Recipe;

namespace SmartSolutionsLab.Yumney.Recipes.Api.Requests;

public sealed record UpdateRecipeRequest(
    string Title,
    string? Description,
    List<SaveRecipeIngredientRequest> Ingredients,
    List<SaveRecipeStepRequest> Steps,
    int? Servings,
    int? PrepTimeMinutes,
    int? CookTimeMinutes,
    string? Difficulty,
    string? ImageUrl,
    List<string>? Tags = null)
{
    public (
        RecipeTitle Title,
        IReadOnlyList<SaveRecipeIngredientItem> Ingredients,
        IReadOnlyList<SaveRecipeStepItem> Steps,
        RecipeDescription? Description,
        Servings? Servings,
        TimingInfo? Timing,
        Difficulty? Difficulty,
        ImageUrl? ImageUrl,
        IReadOnlyList<RecipeTag>? Tags) ToValueObjects() =>
    (
        RecipeTitle.From(Title),
        Ingredients.MapToRecipeIngredientItems().ToList(),
        Steps.MapToRecipeStepItems().ToList(),
        RecipeDescription.FromNullable(Description),
        Domain.Recipe.Servings.FromNullable(Servings),
        TimingInfo.FromNullable(
            PreparationTime.FromNullable(PrepTimeMinutes),
            CookingTime.FromNullable(CookTimeMinutes)),
        Domain.Recipe.Difficulty.FromNullable(Difficulty),
        Domain.Recipe.ImageUrl.FromNullable(ImageUrl),
        Tags?.Select(t => RecipeTag.From(t)).ToList());
}
