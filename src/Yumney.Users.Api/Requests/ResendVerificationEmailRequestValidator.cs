using FluentValidation;
using SmartSolutionsLab.Yumney.Users.Domain.AppUserProfile;

namespace SmartSolutionsLab.Yumney.Users.Api.Requests;

public sealed class ResendVerificationEmailRequestValidator : AbstractValidator<ResendVerificationEmailRequest>
{
	public ResendVerificationEmailRequestValidator()
	{
		RuleFor(x => x.Email)
			.NotEmpty()
			.MaximumLength(Email.MaxLength)
			.EmailAddress();
	}
}
