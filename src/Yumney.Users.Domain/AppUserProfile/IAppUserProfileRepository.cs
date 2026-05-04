namespace SmartSolutionsLab.Yumney.Users.Domain.AppUserProfile;

public interface IAppUserProfileRepository
{
	Task<AppUserProfile?> FindByKeycloakUserIdAsync(KeycloakUserId keycloakUserId, CancellationToken cancellationToken = default);

	// Tracked fetch for update flows. Throws EntityNotFoundException if not found.
	Task<AppUserProfile> GetByKeycloakUserIdAsync(KeycloakUserId keycloakUserId, CancellationToken cancellationToken = default);

	Task AddAsync(AppUserProfile profile, CancellationToken cancellationToken = default);

	// Idempotent: returns the number of rows removed (0 if already gone).
	// Used by the GDPR account-deletion flow (US-101).
	Task<int> DeleteByKeycloakUserIdAsync(KeycloakUserId keycloakUserId, CancellationToken cancellationToken = default);
}
