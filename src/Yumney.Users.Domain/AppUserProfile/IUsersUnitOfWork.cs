using SmartSolutionsLab.Yumney.Shared.Persistence;
using SmartSolutionsLab.Yumney.Users.Domain.StaplesList;
using SmartSolutionsLab.Yumney.Users.Domain.UserActivity;

namespace SmartSolutionsLab.Yumney.Users.Domain.AppUserProfile;

public interface IUsersUnitOfWork : IUnitOfWork
{
	IAppUserProfileRepository Profiles { get; }

	IUserActivityRepository Activities { get; }

	IStaplesListRepository StaplesLists { get; }
}
