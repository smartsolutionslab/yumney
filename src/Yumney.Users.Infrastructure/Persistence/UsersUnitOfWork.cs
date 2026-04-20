using SmartSolutionsLab.Yumney.Users.Domain.AppUserProfile;

namespace SmartSolutionsLab.Yumney.Users.Infrastructure.Persistence;

public sealed class UsersUnitOfWork(UsersDbContext context, IAppUserProfileRepository profiles) : IUsersUnitOfWork
{
	public IAppUserProfileRepository Profiles => profiles;

	public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
		=> context.SaveChangesAsync(cancellationToken);
}
