namespace SmartSolutionsLab.Yumney.Users.Domain.UserActivity;

public interface IUserActivityRepository
{
	Task AddAsync(UserActivity activity, CancellationToken cancellationToken = default);

	Task<IReadOnlyList<UserActivity>> GetRecentAsync(OwnerIdentifier owner, ActivityLimit limit, CancellationToken cancellationToken = default);

	// Idempotent. Returns the number of rows removed.
	Task<int> DeleteAllByOwnerAsync(OwnerIdentifier owner, CancellationToken cancellationToken = default);
}
