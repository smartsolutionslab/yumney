using Yumney.Shared.Common;
using Yumney.Shared.CQRS;

namespace Yumney.Users.Application.Commands;

public sealed record ResendVerificationEmailCommand(string Email) : ICommand<Result>;
