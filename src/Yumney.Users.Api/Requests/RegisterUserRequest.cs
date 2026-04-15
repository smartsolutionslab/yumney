using SmartSolutionsLab.Yumney.Users.Domain.AppUserProfile;

namespace SmartSolutionsLab.Yumney.Users.Api.Requests;

public sealed record RegisterUserRequest(string Email, string Password, string DisplayName)
{
    public (Domain.AppUserProfile.Email Email, Domain.AppUserProfile.Password Password, Domain.AppUserProfile.DisplayName DisplayName) ToValueObjects() =>
        (Domain.AppUserProfile.Email.From(Email), Domain.AppUserProfile.Password.From(Password), Domain.AppUserProfile.DisplayName.From(DisplayName));
}
