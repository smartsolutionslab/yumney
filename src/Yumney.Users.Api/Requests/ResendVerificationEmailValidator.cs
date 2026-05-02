using FluentValidation;
using SmartSolutionsLab.Yumney.Users.Domain.AppUserProfile;

namespace SmartSolutionsLab.Yumney.Users.Api.Requests;

public sealed class ResendVerificationEmailValidator : AbstractValidator<ResendVerificationEmail>
{
	public ResendVerificationEmailValidator()
	{
		RuleFor(x => x.Email)
			.NotEmpty()
			.MaximumLength(Email.MaxLength)
			.EmailAddress();
	}
}
