using Yumney.Shared.Common;
using Yumney.Shared.CQRS;
using Yumney.Users.Domain.AppUserProfile;

namespace Yumney.Users.Application.Commands;

public sealed record ResendVerificationEmailCommand(Email Email) : ICommand<Result>;
