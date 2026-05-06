namespace SmartSolutionsLab.Yumney.Users.Domain.UserActivity;

public interface IUserActivityRepository
{
	Task AddAsync(UserActivity activity, CancellationToken cancellationToken = default);

	Task<IReadOnlyList<UserActivity>> GetRecentAsync(OwnerIdentifier owner, ActivityLimit limit, CancellationToken cancellationToken = default);

	// Filter-by-type variant for the activity timeline UI (US-121).
	Task<IReadOnlyList<UserActivity>> GetRecentByTypeAsync(
		OwnerIdentifier owner,
		ActivityType type,
		ActivityLimit limit,
		CancellationToken cancellationToken = default);

	// Per-recipe stats: cook count + last cooked + view count (US-121).
	Task<RecipeActivityStats> GetRecipeStatsAsync(
		OwnerIdentifier owner,
		RecipeIdentifierSnapshot recipeIdentifier,
		CancellationToken cancellationToken = default);

	// Stages a delete via the change tracker; caller commits via IUnitOfWork.SaveChangesAsync.
	// Idempotent — no-op if no activity rows exist for the owner.
	Task DeleteAllByOwnerAsync(OwnerIdentifier owner, CancellationToken cancellationToken = default);
}

public sealed record RecipeActivityStats(int CookCount, DateTime? LastCookedAt, int ViewCount);
