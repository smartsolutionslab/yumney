namespace SmartSolutionsLab.Yumney.Users.Domain.AppUserProfile;

public interface IAppUserProfileRepository
{
    Task<AppUserProfile?> FindByKeycloakUserIdAsync(KeycloakUserId keycloakUserId, CancellationToken cancellationToken = default);

    // Tracked fetch for update flows. Throws EntityNotFoundException if not found.
    Task<AppUserProfile> GetByKeycloakUserIdAsync(KeycloakUserId keycloakUserId, CancellationToken cancellationToken = default);

    Task AddAsync(AppUserProfile profile, CancellationToken cancellationToken = default);
}
