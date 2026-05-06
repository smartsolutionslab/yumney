namespace SmartSolutionsLab.Yumney.Users.Domain.AppUserProfile;

public interface IAppUserProfileRepository
{
	Task<AppUserProfile?> FindByKeycloakUserIdAsync(KeycloakUserId keycloakUserId, CancellationToken cancellationToken = default);

	// Tracked fetch for update flows. Throws EntityNotFoundException if not found.
	Task<AppUserProfile> GetByKeycloakUserIdAsync(KeycloakUserId keycloakUserId, CancellationToken cancellationToken = default);

	Task AddAsync(AppUserProfile profile, CancellationToken cancellationToken = default);

	// Stages a delete via the change tracker; caller commits via IUnitOfWork.SaveChangesAsync.
	// Idempotent — no-op if no profile matches. Used by the GDPR account-deletion flow (US-101).
	Task DeleteByKeycloakUserIdAsync(KeycloakUserId keycloakUserId, CancellationToken cancellationToken = default);
}
