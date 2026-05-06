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
}
