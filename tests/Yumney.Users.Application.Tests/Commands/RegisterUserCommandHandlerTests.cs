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
        // Arrange
        var command = new RegisterUserCommand("test@example.com", "Password1", "Test User");
        var keycloakUserId = Guid.NewGuid().ToString();

        keycloakAdmin
            .CreateUserAsync(command.Email, command.Password, command.DisplayName, Arg.Any<CancellationToken>())
            .Returns(Result<string>.Success(keycloakUserId));

        // Act
        var result = await sut.HandleAsync(command);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Message.Should().Contain("check your email");

        await users.Received(1).AddAsync(
            Arg.Is<AppUserProfile>(p =>
                p.KeycloakUserId == keycloakUserId &&
                p.DisplayName == "Test User"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_KeycloakReturnsFailure_ReturnsFailureWithoutCreatingProfile()
    {
        // Arrange
        var command = new RegisterUserCommand("test@example.com", "Password1", "Test User");

        keycloakAdmin
            .CreateUserAsync(command.Email, command.Password, command.DisplayName, Arg.Any<CancellationToken>())
            .Returns(Result<string>.Failure(RegistrationErrors.EmailAlreadyExists));

        // Act
        var result = await sut.HandleAsync(command);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(RegistrationErrors.EmailAlreadyExists);

        await users.DidNotReceive().AddAsync(
            Arg.Any<AppUserProfile>(),
            Arg.Any<CancellationToken>());
    }
}
