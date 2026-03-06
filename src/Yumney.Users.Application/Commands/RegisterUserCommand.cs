using Yumney.Shared.Common;
using Yumney.Shared.CQRS;

namespace Yumney.Users.Application.Commands;

public sealed record RegisterUserCommand(
    string Email,
    string Password,
    string DisplayName) : ICommand<Result<RegisterUserResultDto>>;

public sealed record RegisterUserResultDto(string Message);
