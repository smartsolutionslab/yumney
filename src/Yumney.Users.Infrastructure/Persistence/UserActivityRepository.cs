using Microsoft.EntityFrameworkCore;
using SmartSolutionsLab.Yumney.Users.Domain.UserActivity;

namespace SmartSolutionsLab.Yumney.Users.Infrastructure.Persistence;

public sealed class UserActivityRepository(UsersDbContext context) : IUserActivityRepository
{
    private readonly DbSet<UserActivity> activities = context.Set<UserActivity>();

    public async Task AddAsync(UserActivity activity, CancellationToken cancellationToken = default)
    {
        await activities.AddAsync(activity, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<UserActivity>> GetRecentAsync(
        OwnerIdentifier owner,
        int limit = 5,
        CancellationToken cancellationToken = default)
    {
        return await activities
            .AsNoTracking()
            .Where(a => a.Owner == owner)
            .OrderByDescending(a => a.OccurredAt)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }
}
