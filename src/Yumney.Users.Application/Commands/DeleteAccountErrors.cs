using SmartSolutionsLab.Yumney.Shared.Outcomes;

namespace SmartSolutionsLab.Yumney.Users.Application.Commands;

public static class DeleteAccountErrors
{
	public static readonly ApiError IdentityProviderUnavailable = new(
		"DELETE_ACCOUNT_IDP_UNAVAILABLE",
		"Local data was erased but the identity provider could not be reached. Please try again or contact support.",
		503);
}
