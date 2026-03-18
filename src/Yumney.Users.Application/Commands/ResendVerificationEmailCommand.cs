using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shared.CQRS;
using SmartSolutionsLab.Yumney.Users.Domain.AppUserProfile;

namespace SmartSolutionsLab.Yumney.Users.Application.Commands;

public sealed record ResendVerificationEmailCommand(Email Email) : ICommand<Result>
{
    public static ResendVerificationEmailCommand From(ResendVerificationEmailRequest request)
    {
        return new ResendVerificationEmailCommand(new Email(request.Email));
    }
}
