namespace Yumney.Users.Application.Commands;

public static class RegistrationErrors
{
    public const string EmailAlreadyExists = "REGISTRATION_EMAIL_EXISTS";

    public const string IdentityProviderUnavailable = "REGISTRATION_IDP_UNAVAILABLE";

    public const string UserCreationFailed = "REGISTRATION_CREATION_FAILED";
}
