using SmartSolutionsLab.Yumney.Shared.Common;

namespace SmartSolutionsLab.Yumney.Users.Domain.AppUserProfile;

public interface IUsersUnitOfWork : IUnitOfWork
{
	IAppUserProfileRepository Profiles { get; }
}
