using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;
using Yumney.Shared.Common;
using Yumney.Users.Application.Commands;
using Yumney.Users.Application.Interfaces;
using Yumney.Users.Domain.AppUserProfile;

namespace Yumney.Users.Application.Tests.Commands;

public class RegisterUserCommandHandlerTests
{
    private static readonly Email TestEmail = new("test@example.com");
    private static readonly Password TestPassword = new("Password1");
    private static readonly DisplayName TestDisplayName = new("Test User");

    private readonly IKeycloakAdminService keycloakAdmin = Substitute.For<IKeycloakAdminService>();
    private readonly IAppUserProfileRepository users = Substitute.For<IAppUserProfileRepository>();
    private readonly ILogger<RegisterUserCommandHandler> logger =
        Substitute.For<ILogger<RegisterUserCommandHandler>>();

    private readonly RegisterUserCommandHandler sut;

    public RegisterUserCommandHandlerTests()
    {
        sut = new RegisterUserCommandHandler(keycloakAdmin, users, logger);
    }

    [Fact]
    public async Task HandleAsync_ValidCommand_ReturnsSuccessAndCreatesProfile()
    {
        var command = new RegisterUserCommand(TestEmail, TestPassword, TestDisplayName);
        var keycloakUserId = new KeycloakUserId(Guid.NewGuid().ToString());

        keycloakAdmin
            .CreateUserAsync(command.Email, command.Password, command.DisplayName, Arg.Any<CancellationToken>())
            .Returns(Result<KeycloakUserId>.Success(keycloakUserId));

        var result = await sut.HandleAsync(command);

        result.IsSuccess.Should().BeTrue();
        result.Value.Message.Should().Contain("check your email");

        await users.Received(1).AddAsync(
            Arg.Is<AppUserProfile>(p =>
                p.KeycloakUserId == keycloakUserId &&
                p.DisplayName == TestDisplayName),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_KeycloakReturnsFailure_ReturnsFailureWithoutCreatingProfile()
    {
        var command = new RegisterUserCommand(TestEmail, TestPassword, TestDisplayName);

        keycloakAdmin
            .CreateUserAsync(command.Email, command.Password, command.DisplayName, Arg.Any<CancellationToken>())
            .Returns(Result<KeycloakUserId>.Failure(RegistrationErrors.EmailAlreadyExists));

        var result = await sut.HandleAsync(command);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(RegistrationErrors.EmailAlreadyExists);

        await users.DidNotReceive().AddAsync(
            Arg.Any<AppUserProfile>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_IdentityProviderUnavailable_ReturnsFailureWithCorrectError()
    {
        var command = new RegisterUserCommand(TestEmail, TestPassword, TestDisplayName);

        keycloakAdmin
            .CreateUserAsync(command.Email, command.Password, command.DisplayName, Arg.Any<CancellationToken>())
            .Returns(Result<KeycloakUserId>.Failure(RegistrationErrors.IdentityProviderUnavailable));

        var result = await sut.HandleAsync(command);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(RegistrationErrors.IdentityProviderUnavailable);
    }

    [Fact]
    public async Task HandleAsync_UserCreationFailed_ReturnsFailureWithCorrectError()
    {
        var command = new RegisterUserCommand(TestEmail, TestPassword, TestDisplayName);

        keycloakAdmin
            .CreateUserAsync(command.Email, command.Password, command.DisplayName, Arg.Any<CancellationToken>())
            .Returns(Result<KeycloakUserId>.Failure(RegistrationErrors.UserCreationFailed));

        var result = await sut.HandleAsync(command);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(RegistrationErrors.UserCreationFailed);
    }

    [Fact]
    public async Task HandleAsync_KeycloakReturnsFailure_DoesNotCallAddAsync()
    {
        var command = new RegisterUserCommand(TestEmail, TestPassword, TestDisplayName);

        keycloakAdmin
            .CreateUserAsync(command.Email, command.Password, command.DisplayName, Arg.Any<CancellationToken>())
            .Returns(Result<KeycloakUserId>.Failure(RegistrationErrors.IdentityProviderUnavailable));

        await sut.HandleAsync(command);

        await users.DidNotReceive().AddAsync(
            Arg.Any<AppUserProfile>(),
            Arg.Any<CancellationToken>());
    }
}
