namespace SmartSolutionsLab.Yumney.MealPlan.Application.Interfaces;

public interface IRecipeCatalogProvider
{
	Task<IReadOnlyList<RecipeCatalogEntry>> ListAsync(int pageSize, CancellationToken cancellationToken = default);
}

public sealed record RecipeCatalogEntry(
	Guid RecipeIdentifier,
	string Title,
	int? PrepTimeMinutes,
	int? CookTimeMinutes,
	string? Difficulty,
	IReadOnlyList<string> Tags,
	bool IsFavorite,
	int? Rating);
