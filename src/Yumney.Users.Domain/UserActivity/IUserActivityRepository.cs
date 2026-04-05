namespace SmartSolutionsLab.Yumney.Users.Domain.UserActivity;

public interface IUserActivityRepository
{
    Task AddAsync(UserActivity activity, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<UserActivity>> GetRecentAsync(
        OwnerIdentifier owner,
        int limit = 5,
        CancellationToken cancellationToken = default);
}
