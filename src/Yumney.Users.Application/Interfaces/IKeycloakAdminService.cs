using SmartSolutionsLab.Yumney.Shared.Outcomes;
using SmartSolutionsLab.Yumney.Users.Domain.AppUserProfile;

namespace SmartSolutionsLab.Yumney.Users.Application.Interfaces;

public interface IKeycloakAdminService
{
	Task<Result<KeycloakUserId>> CreateUserAsync(Email email, Password password, DisplayName displayName, CancellationToken cancellationToken = default);

	Task<Result<KeycloakUserId>> FindUserByEmailAsync(Email email, CancellationToken cancellationToken = default);

	Task<Result> SendVerificationEmailAsync(KeycloakUserId keycloakUserId, CancellationToken cancellationToken = default);

	/// <summary>
	/// Permanently removes the user from the Keycloak realm. Treats a 404 from Keycloak
	/// as success so the call is idempotent — the caller can retry safely after a
	/// partial failure.
	/// </summary>
	/// <param name="keycloakUserId">The Keycloak user identifier to delete.</param>
	/// <param name="cancellationToken">Token to cancel the in-flight request.</param>
	/// <returns>Success on deletion (or 404); failure when Keycloak is unreachable.</returns>
	Task<Result> DeleteUserAsync(KeycloakUserId keycloakUserId, CancellationToken cancellationToken = default);

	/// <summary>
	/// Looks up the user's email in Keycloak. Used by the GDPR account-deletion
	/// flow to capture the destination address before the local profile row is
	/// purged. Returns a not-found error when the user is missing on Keycloak —
	/// the caller decides whether to proceed without sending a confirmation.
	/// </summary>
	/// <param name="keycloakUserId">The Keycloak user identifier to look up.</param>
	/// <param name="cancellationToken">Token to cancel the in-flight request.</param>
	/// <returns>The user's email when known; failure otherwise.</returns>
	Task<Result<Email>> GetEmailAsync(KeycloakUserId keycloakUserId, CancellationToken cancellationToken = default);
}
