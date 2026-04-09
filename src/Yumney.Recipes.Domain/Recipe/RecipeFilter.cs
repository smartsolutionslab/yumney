namespace SmartSolutionsLab.Yumney.Recipes.Domain.Recipe;

/// <summary>
/// Optional filter criteria applied to <see cref="IRecipeRepository.GetByOwnerAsync"/>.
/// All non-null values are combined with AND logic. Tags are matched as a
/// "must contain all" set, not "must contain any".
/// </summary>
public sealed record RecipeFilter(
    IReadOnlyList<RecipeTag>? Tags = null,
    Difficulty? Difficulty = null,
    PreparationTime? MaxPrepTime = null,
    CookingTime? MaxCookTime = null,
    bool? FavoritesOnly = null)
{
    public bool IsEmpty =>
        (Tags is null || Tags.Count == 0)
        && Difficulty is null
        && MaxPrepTime is null
        && MaxCookTime is null
        && FavoritesOnly is not true;
}
