using FluentValidation;

namespace SmartSolutionsLab.Yumney.Users.Application.Commands;

public sealed class ResendVerificationEmailRequestValidator : AbstractValidator<ResendVerificationEmailRequest>
{
    public ResendVerificationEmailRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .MaximumLength(254)
            .EmailAddress();
    }
}
