using SmartSolutionsLab.Yumney.Shared.Persistence;

namespace SmartSolutionsLab.Yumney.Users.Domain.AppUserProfile;

public interface IUsersUnitOfWork : IUnitOfWork
{
	IAppUserProfileRepository Profiles { get; }
}
