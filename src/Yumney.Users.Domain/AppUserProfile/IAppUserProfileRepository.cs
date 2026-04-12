namespace SmartSolutionsLab.Yumney.Users.Domain.AppUserProfile;

public interface IAppUserProfileRepository
{
    Task<AppUserProfile?> FindByKeycloakUserIdAsync(KeycloakUserId keycloakUserId, CancellationToken cancellationToken = default);

    // Tracked fetch for update flows. Throws EntityNotFoundException if not found.
    Task<AppUserProfile> GetByKeycloakUserIdAsync(KeycloakUserId keycloakUserId, CancellationToken cancellationToken = default);

    Task AddAsync(AppUserProfile profile, CancellationToken cancellationToken = default);

    /// <summary>
    /// Save changes to a tracked profile.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the async operation.</returns>
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
