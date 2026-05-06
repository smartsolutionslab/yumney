using SmartSolutionsLab.Yumney.Shared.Persistence;
using SmartSolutionsLab.Yumney.Users.Domain.AppUserProfile;
using SmartSolutionsLab.Yumney.Users.Domain.StaplesList;
using SmartSolutionsLab.Yumney.Users.Domain.UserActivity;

namespace SmartSolutionsLab.Yumney.Users.Infrastructure.Persistence;

public sealed class UsersUnitOfWork(UsersDbContext context)
	: UnitOfWork<UsersDbContext>(context), IUsersUnitOfWork
{
	private IAppUserProfileRepository? profilesRepository;
	private IUserActivityRepository? activitiesRepository;
	private IStaplesListRepository? staplesListsRepository;

	public IAppUserProfileRepository Profiles => profilesRepository ??= new AppUserProfileRepository(Context);

	public IUserActivityRepository Activities => activitiesRepository ??= new UserActivityRepository(Context);

	public IStaplesListRepository StaplesLists => staplesListsRepository ??= new StaplesListRepository(Context);
}
