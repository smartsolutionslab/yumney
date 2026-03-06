using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;
using Yumney.Shared.Common;
using Yumney.Users.Application.Commands;
using Yumney.Users.Application.Interfaces;

namespace Yumney.Users.Application.Tests.Commands;

public class ResendVerificationEmailCommandHandlerTests
{
    private readonly IKeycloakAdminService keycloakAdmin = Substitute.For<IKeycloakAdminService>();
    private readonly ILogger<ResendVerificationEmailCommandHandler> logger =
        Substitute.For<ILogger<ResendVerificationEmailCommandHandler>>();

    private readonly ResendVerificationEmailCommandHandler sut;

    public ResendVerificationEmailCommandHandlerTests()
    {
        sut = new ResendVerificationEmailCommandHandler(keycloakAdmin, logger);
    }

    [Fact]
    public async Task HandleAsync_ValidEmail_ReturnsSuccessAndSendsEmail()
    {
        var command = new ResendVerificationEmailCommand("test@example.com");
        var keycloakUserId = Guid.NewGuid().ToString();

        keycloakAdmin
            .FindUserByEmailAsync(command.Email, Arg.Any<CancellationToken>())
            .Returns(Result<string>.Success(keycloakUserId));

        keycloakAdmin
            .SendVerificationEmailAsync(keycloakUserId, Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        var result = await sut.HandleAsync(command);

        result.IsSuccess.Should().BeTrue();

        await keycloakAdmin.Received(1).SendVerificationEmailAsync(
            keycloakUserId,
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_UserNotFound_ReturnsSuccessToPreventEnumeration()
    {
        var command = new ResendVerificationEmailCommand("unknown@example.com");

        keycloakAdmin
            .FindUserByEmailAsync(command.Email, Arg.Any<CancellationToken>())
            .Returns(Result<string>.Failure(VerificationErrors.UserNotFound));

        var result = await sut.HandleAsync(command);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task HandleAsync_UserNotFound_DoesNotAttemptToSendEmail()
    {
        var command = new ResendVerificationEmailCommand("unknown@example.com");

        keycloakAdmin
            .FindUserByEmailAsync(command.Email, Arg.Any<CancellationToken>())
            .Returns(Result<string>.Failure(VerificationErrors.UserNotFound));

        await sut.HandleAsync(command);

        await keycloakAdmin.DidNotReceive().SendVerificationEmailAsync(
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_IdentityProviderUnavailable_ReturnsFailure()
    {
        var command = new ResendVerificationEmailCommand("test@example.com");

        keycloakAdmin
            .FindUserByEmailAsync(command.Email, Arg.Any<CancellationToken>())
            .Returns(Result<string>.Failure(VerificationErrors.IdentityProviderUnavailable));

        var result = await sut.HandleAsync(command);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(VerificationErrors.IdentityProviderUnavailable);
    }

    [Fact]
    public async Task HandleAsync_IdentityProviderUnavailable_DoesNotAttemptToSendEmail()
    {
        var command = new ResendVerificationEmailCommand("test@example.com");

        keycloakAdmin
            .FindUserByEmailAsync(command.Email, Arg.Any<CancellationToken>())
            .Returns(Result<string>.Failure(VerificationErrors.IdentityProviderUnavailable));

        await sut.HandleAsync(command);

        await keycloakAdmin.DidNotReceive().SendVerificationEmailAsync(
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_SendFailed_ReturnsFailure()
    {
        var command = new ResendVerificationEmailCommand("test@example.com");
        var keycloakUserId = Guid.NewGuid().ToString();

        keycloakAdmin
            .FindUserByEmailAsync(command.Email, Arg.Any<CancellationToken>())
            .Returns(Result<string>.Success(keycloakUserId));

        keycloakAdmin
            .SendVerificationEmailAsync(keycloakUserId, Arg.Any<CancellationToken>())
            .Returns(Result.Failure(VerificationErrors.SendFailed));

        var result = await sut.HandleAsync(command);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(VerificationErrors.SendFailed);
    }
}
