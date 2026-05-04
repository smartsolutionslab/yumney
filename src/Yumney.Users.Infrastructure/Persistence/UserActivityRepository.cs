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

	public async Task<int> DeleteAllByOwnerAsync(OwnerIdentifier owner, CancellationToken cancellationToken = default)
	{
		return await activities.Where(activity => activity.Owner == owner).ExecuteDeleteAsync(cancellationToken);
	}
}
