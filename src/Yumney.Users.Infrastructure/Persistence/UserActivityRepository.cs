using Microsoft.EntityFrameworkCore;
using SmartSolutionsLab.Yumney.Users.Domain.UserActivity;

namespace SmartSolutionsLab.Yumney.Users.Infrastructure.Persistence;

public sealed class UserActivityRepository(UsersDbContext context) : IUserActivityRepository
{
	private readonly DbSet<UserActivity> activities = context.Set<UserActivity>();

	public async Task AddAsync(UserActivity activity, CancellationToken cancellationToken = default)
	{
		await activities.AddAsync(activity, cancellationToken);
	}

	public async Task<IReadOnlyList<UserActivity>> GetRecentAsync(
		OwnerIdentifier owner,
		ActivityLimit limit,
		CancellationToken cancellationToken = default)
	{
		return await activities
			.AsNoTracking()
			.Where(activity => activity.Owner == owner)
			.OrderByDescending(activity => activity.OccurredAt)
			.Take(limit.Value)
			.ToListAsync(cancellationToken);
	}

	public async Task<IReadOnlyList<UserActivity>> GetRecentByTypeAsync(
		OwnerIdentifier owner,
		ActivityType type,
		ActivityLimit limit,
		CancellationToken cancellationToken = default)
	{
		return await activities
			.AsNoTracking()
			.Where(activity => activity.Owner == owner && activity.Type == type)
			.OrderByDescending(activity => activity.OccurredAt)
			.Take(limit.Value)
			.ToListAsync(cancellationToken);
	}

	public Task<IReadOnlyList<UserActivity>> GetRecentByCursorAsync(
		OwnerIdentifier owner,
		ActivityLimit limit,
		ActivityCursor? cursor,
		CancellationToken cancellationToken = default) =>
		QueryByCursorAsync(activity => activity.Owner == owner, limit, cursor, cancellationToken);

	public Task<IReadOnlyList<UserActivity>> GetRecentByTypeAndCursorAsync(
		OwnerIdentifier owner,
		ActivityType type,
		ActivityLimit limit,
		ActivityCursor? cursor,
		CancellationToken cancellationToken = default) =>
		QueryByCursorAsync(activity => activity.Owner == owner && activity.Type == type, limit, cursor, cancellationToken);

	public async Task<RecipeActivityStats> GetRecipeStatsAsync(
		OwnerIdentifier owner,
		RecipeIdentifierSnapshot recipeIdentifier,
		CancellationToken cancellationToken = default)
	{
		var recipeRows = activities
			.AsNoTracking()
			.Where(activity => activity.Owner == owner && activity.RecipeIdentifier == recipeIdentifier);

		var cooked = recipeRows.Where(activity => activity.Type == ActivityType.RecipeCooked);

		var cookCount = await cooked.CountAsync(cancellationToken);
		var lastCookedAt = await cooked
			.OrderByDescending(activity => activity.OccurredAt)
			.Select(activity => (DateTime?)activity.OccurredAt)
			.FirstOrDefaultAsync(cancellationToken);
		var viewCount = await recipeRows
			.CountAsync(activity => activity.Type == ActivityType.RecipeViewed, cancellationToken);

		return new RecipeActivityStats(cookCount, lastCookedAt, viewCount);
	}

	public async Task DeleteAllByOwnerAsync(OwnerIdentifier owner, CancellationToken cancellationToken = default)
	{
		var matches = await activities.Where(activity => activity.Owner == owner).ToListAsync(cancellationToken);
		activities.RemoveRange(matches);
	}

	private async Task<IReadOnlyList<UserActivity>> QueryByCursorAsync(
		System.Linq.Expressions.Expression<Func<UserActivity, bool>> filter,
		ActivityLimit limit,
		ActivityCursor? cursor,
		CancellationToken cancellationToken)
	{
		var query = activities.AsNoTracking().Where(filter);

		if (cursor is not null)
		{
			// Strictly older than the cursor's tick. The cursor still carries
			// an id for transport stability (clients can de-dupe across page
			// boundaries) but server-side we only compare on OccurredAt —
			// EF Core can't translate < on a value-converted UserActivityIdentifier.
			// Same-tick collisions are vanishingly rare for a per-user activity
			// feed (DateTime.UtcNow ticks at 100ns precision, write rate is
			// human-paced).
			var cursorOccurred = cursor.OccurredAt;
			query = query.Where(activity => activity.OccurredAt < cursorOccurred);
		}

		return await query
			.OrderByDescending(activity => activity.OccurredAt)
			.Take(limit.Value)
			.ToListAsync(cancellationToken);
	}
}
