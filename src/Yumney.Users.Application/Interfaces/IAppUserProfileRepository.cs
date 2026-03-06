using Yumney.Users.Domain.AppUserProfile;

namespace Yumney.Users.Application.Interfaces;

public interface IAppUserProfileRepository
{
    Task<AppUserProfile?> FindByKeycloakUserIdAsync(string keycloakUserId, CancellationToken cancellationToken = default);

    Task AddAsync(AppUserProfile profile, CancellationToken cancellationToken = default);
}
