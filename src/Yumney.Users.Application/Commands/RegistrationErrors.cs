using SmartSolutionsLab.Yumney.Shared.Common;

namespace SmartSolutionsLab.Yumney.Users.Application.Commands;

public static class RegistrationErrors
{
	public static readonly ApiError EmailAlreadyExists = new("REGISTRATION_EMAIL_EXISTS", "A user with this email address already exists.", 409);

	public static readonly ApiError IdentityProviderUnavailable = new("REGISTRATION_IDP_UNAVAILABLE", "Identity provider is unavailable.", 503);

	public static readonly ApiError UserCreationFailed = new("REGISTRATION_CREATION_FAILED", "Failed to create user account.", 500);
}
