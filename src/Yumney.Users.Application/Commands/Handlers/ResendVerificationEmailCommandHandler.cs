using Microsoft.Extensions.Logging;
using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shared.CQRS;
using SmartSolutionsLab.Yumney.Users.Application.Interfaces;

namespace SmartSolutionsLab.Yumney.Users.Application.Commands.Handlers;

#pragma warning disable SA1601 // Partial elements should be documented (required for LoggerMessage source generation)
public sealed partial class ResendVerificationEmailCommandHandler(IKeycloakAdminService keycloakAdmin, ILogger<ResendVerificationEmailCommandHandler> logger)
    : ICommandHandler<ResendVerificationEmailCommand, Result>
{
    public async Task<Result> HandleAsync(ResendVerificationEmailCommand command, CancellationToken cancellationToken = default)
    {
        var email = command.Email;

        var findResult = await keycloakAdmin.FindUserByEmailAsync(email, cancellationToken);

        if (findResult.IsFailure)
        {
            if (findResult.Error == VerificationErrors.UserNotFound)
            {
                LogUserNotFoundSilent(email.Value);
                return Result.Success();
            }

            return Result.Failure(findResult.Error!);
        }

        var keycloakUserId = findResult.Value;

        var sendResult = await keycloakAdmin.SendVerificationEmailAsync(keycloakUserId, cancellationToken);

        if (sendResult.IsFailure)
        {
            return Result.Failure(sendResult.Error!);
        }

        LogVerificationEmailSent(email.Value);

        return Result.Success();
    }

    [LoggerMessage(Level = LogLevel.Debug, Message = "Resend verification requested for unknown email {Email}, returning success to prevent enumeration")]
    private partial void LogUserNotFoundSilent(string email);

    [LoggerMessage(Level = LogLevel.Information, Message = "Verification email resent for {Email}")]
    private partial void LogVerificationEmailSent(string email);
}
