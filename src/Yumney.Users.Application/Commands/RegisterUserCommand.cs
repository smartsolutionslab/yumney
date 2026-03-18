using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shared.CQRS;
using SmartSolutionsLab.Yumney.Users.Domain.AppUserProfile;

namespace SmartSolutionsLab.Yumney.Users.Application.Commands;

public sealed record RegisterUserCommand(
    Email Email,
    Password Password,
    DisplayName DisplayName) : ICommand<Result<RegisterUserResultDto>>
{
    public static RegisterUserCommand From(RegisterUserRequest request)
    {
        var (email, password, displayName) = request;
        return new RegisterUserCommand(new Email(email), new Password(password), new DisplayName(displayName));
    }
}
