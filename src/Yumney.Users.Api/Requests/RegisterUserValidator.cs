using FluentValidation;
using SmartSolutionsLab.Yumney.Users.Domain.AppUserProfile;

namespace SmartSolutionsLab.Yumney.Users.Api.Requests;

public sealed class RegisterUserValidator : AbstractValidator<RegisterUser>
{
	public RegisterUserValidator()
	{
		RuleFor(request => request.Email)
			.NotEmpty()
			.MaximumLength(Email.MaxLength)
			.EmailAddress();

		RuleFor(request => request.Password)
			.NotEmpty()
			.MinimumLength(Password.MinLength)
			.Matches(Password.UppercasePattern).WithMessage(Password.UppercaseMessage)
			.Matches(Password.LowercasePattern).WithMessage(Password.LowercaseMessage)
			.Matches(Password.DigitPattern).WithMessage(Password.DigitMessage);

		RuleFor(request => request.DisplayName)
			.NotEmpty()
			.MaximumLength(DisplayName.MaxLength);
	}
}
