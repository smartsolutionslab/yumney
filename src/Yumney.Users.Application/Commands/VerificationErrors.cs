using SmartSolutionsLab.Yumney.Shared.Outcomes;

namespace SmartSolutionsLab.Yumney.Users.Application.Commands;

public static class VerificationErrors
{
	public static readonly ApiError UserNotFound = new("VERIFICATION_USER_NOT_FOUND", "User not found.", 404);

	public static readonly ApiError IdentityProviderUnavailable = new("VERIFICATION_IDP_UNAVAILABLE", "Identity provider is unavailable.", 503);

	public static readonly ApiError SendFailed = new("VERIFICATION_SEND_FAILED", "Failed to send verification email.", 500);
}
