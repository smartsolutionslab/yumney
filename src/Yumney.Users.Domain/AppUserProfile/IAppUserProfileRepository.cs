namespace Yumney.Users.Domain.AppUserProfile;

public interface IAppUserProfileRepository
{
    Task<AppUserProfile?> FindByKeycloakUserIdAsync(KeycloakUserId keycloakUserId, CancellationToken cancellationToken = default);

    Task AddAsync(AppUserProfile profile, CancellationToken cancellationToken = default);
}
