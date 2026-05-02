using SmartSolutionsLab.Yumney.Users.Domain.AppUserProfile;

namespace SmartSolutionsLab.Yumney.Users.Api.Requests;

public sealed record RegisterUser(string Email, string Password, string DisplayName)
{
	public (Email Email, Password Password, DisplayName DisplayName) ToValueObjects() =>
		(Domain.AppUserProfile.Email.From(Email), Domain.AppUserProfile.Password.From(Password), Domain.AppUserProfile.DisplayName.From(DisplayName));
}
