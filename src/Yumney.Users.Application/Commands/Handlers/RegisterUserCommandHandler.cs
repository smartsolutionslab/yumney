using Microsoft.Extensions.Logging;
using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shared.CQRS;
using SmartSolutionsLab.Yumney.Users.Application.DTOs;
using SmartSolutionsLab.Yumney.Users.Application.Interfaces;
using SmartSolutionsLab.Yumney.Users.Domain.AppUserProfile;

namespace SmartSolutionsLab.Yumney.Users.Application.Commands.Handlers;

#pragma warning disable SA1601 // Partial elements should be documented (required for LoggerMessage source generation)
public sealed partial class RegisterUserCommandHandler(
    IKeycloakAdminService keycloakAdmin,
    IAppUserProfileRepository users,
    ILogger<RegisterUserCommandHandler> logger) : ICommandHandler<RegisterUserCommand, Result<RegisterUserResultDto>>
{
    public async Task<Result<RegisterUserResultDto>> HandleAsync(
        RegisterUserCommand command,
        CancellationToken cancellationToken = default)
    {
        var (email, password, displayName) = command;

        var keycloakResult = await keycloakAdmin.CreateUserAsync(email, password, displayName, cancellationToken);

        if (keycloakResult.IsFailure) return Result<RegisterUserResultDto>.Failure(keycloakResult.Error!);

        var keycloakUserId = keycloakResult.Value;

        var profile = AppUserProfile.Create(keycloakUserId, displayName);
        await users.AddAsync(profile, cancellationToken);

        LogUserRegistered(email.Value, keycloakUserId.Value);

        return Result<RegisterUserResultDto>.Success(new RegisterUserResultDto("Registration successful. Please check your email to verify your account."));
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "User {Email} registered with Keycloak ID {KeycloakUserId}")]
    private partial void LogUserRegistered(string email, string keycloakUserId);
}
