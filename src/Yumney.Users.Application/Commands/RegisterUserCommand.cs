using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shared.CQRS;
using SmartSolutionsLab.Yumney.Users.Domain.AppUserProfile;

namespace SmartSolutionsLab.Yumney.Users.Application.Commands;

public sealed record RegisterUserCommand(
    Email Email,
    Password Password,
    DisplayName DisplayName) : ICommand<Result<RegisterUserResultDto>>;

public sealed record RegisterUserResultDto(string Message);
