using SmartSolutionsLab.Yumney.Recipes.Application.Commands;
using SmartSolutionsLab.Yumney.Recipes.Domain.Recipe;

namespace SmartSolutionsLab.Yumney.Recipes.Api.Requests;

public sealed record SaveRecipeRequest(
    string Title,
    string? Description,
    List<SaveRecipeIngredientRequest> Ingredients,
    List<SaveRecipeStepRequest> Steps,
    int? Servings,
    int? PrepTimeMinutes,
    int? CookTimeMinutes,
    string? Difficulty,
    string? ImageUrl,
    string? Language = null,
    string? SourceUrl = null,
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
        RecipeLanguage? Language,
        RecipeUrl? SourceUrl,
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
        RecipeLanguage.FromNullable(Language),
        RecipeUrl.FromNullable(SourceUrl),
        Tags?.Select(t => RecipeTag.From(t)).ToList());

    public void Deconstruct(
        out RecipeTitle title,
        out IReadOnlyList<SaveRecipeIngredientItem> ingredients,
        out IReadOnlyList<SaveRecipeStepItem> steps,
        out RecipeDescription? description,
        out Servings? servings,
        out TimingInfo? timing,
        out Difficulty? difficulty,
        out ImageUrl? imageUrl,
        out RecipeLanguage? language,
        out RecipeUrl? sourceUrl,
        out IReadOnlyList<RecipeTag>? tags)
    {
        (title, ingredients, steps, description, servings, timing, difficulty, imageUrl, language, sourceUrl, tags) =
            ToValueObjects();
    }
}
