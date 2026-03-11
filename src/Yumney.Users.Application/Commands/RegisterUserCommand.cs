using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shared.CQRS;
using SmartSolutionsLab.Yumney.Users.Domain.AppUserProfile;

namespace SmartSolutionsLab.Yumney.Users.Application.Commands;

public sealed record RegisterUserCommand(
    Email Email,
    Password Password,
    DisplayName DisplayName) : ICommand<Result<RegisterUserResultDto>>
{
    public static RegisterUserCommand FromRequest(string email, string password, string displayName)
        => new(new Email(email), new Password(password), new DisplayName(displayName));
}
