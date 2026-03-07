using Yumney.Shared.Common;
using Yumney.Shared.CQRS;
using Yumney.Users.Domain.AppUserProfile;

namespace Yumney.Users.Application.Commands;

public sealed record RegisterUserCommand(
    Email Email,
    Password Password,
    DisplayName DisplayName) : ICommand<Result<RegisterUserResultDto>>;

public sealed record RegisterUserResultDto(string Message);
