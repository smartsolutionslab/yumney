using FluentValidation;
using SmartSolutionsLab.Yumney.Users.Domain.AppUserProfile;

namespace SmartSolutionsLab.Yumney.Users.Api.Requests;

public sealed class RegisterUserRequestValidator : AbstractValidator<RegisterUserRequest>
{
    public RegisterUserRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .MaximumLength(Email.MaxLength)
            .EmailAddress();

        RuleFor(x => x.Password)
            .NotEmpty()
            .MinimumLength(Password.MinLength)
            .Matches(Password.UppercasePattern).WithMessage(Password.UppercaseMessage)
            .Matches(Password.LowercasePattern).WithMessage(Password.LowercaseMessage)
            .Matches(Password.DigitPattern).WithMessage(Password.DigitMessage);

        RuleFor(x => x.DisplayName)
            .NotEmpty()
            .MaximumLength(DisplayName.MaxLength);
    }
}
