using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shared.CQRS;
using SmartSolutionsLab.Yumney.Users.Domain.AppUserProfile;

namespace SmartSolutionsLab.Yumney.Users.Application.Commands;

public sealed record ResendVerificationEmailCommand(Email Email) : ICommand<Result>
{
    public static ResendVerificationEmailCommand From(string email) => new(new Email(email));
}
