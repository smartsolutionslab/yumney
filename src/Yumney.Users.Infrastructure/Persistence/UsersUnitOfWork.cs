using SmartSolutionsLab.Yumney.Users.Domain.AppUserProfile;
using SmartSolutionsLab.Yumney.Users.Domain.StaplesList;
using SmartSolutionsLab.Yumney.Users.Domain.UserActivity;

namespace SmartSolutionsLab.Yumney.Users.Infrastructure.Persistence;

public sealed class UsersUnitOfWork(UsersDbContext context) : IUsersUnitOfWork
{
	public IAppUserProfileRepository Profiles => field ??= new AppUserProfileRepository(context);

	public IUserActivityRepository Activities => field ??= new UserActivityRepository(context);

	public IStaplesListRepository StaplesLists => field ??= new StaplesListRepository(context);

	public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
		=> context.SaveChangesAsync(cancellationToken);
}
