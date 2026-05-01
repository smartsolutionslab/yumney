using FluentAssertions;
using NSubstitute;
using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Users.Application.Commands;
using SmartSolutionsLab.Yumney.Users.Application.Commands.Handlers;
using SmartSolutionsLab.Yumney.Users.Application.Interfaces;
using SmartSolutionsLab.Yumney.Users.Domain.AppUserProfile;
using Xunit;

namespace SmartSolutionsLab.Yumney.Users.Application.Tests.Commands;

public class ResendVerificationEmailCommandHandlerTests
{
	private static readonly Email TestEmail = Email.From("test@example.com");
	private static readonly Email UnknownEmail = Email.From("unknown@example.com");

	private readonly IKeycloakAdminService keycloakAdmin = Substitute.For<IKeycloakAdminService>();

	private readonly ResendVerificationEmailCommandHandler handler;

	public ResendVerificationEmailCommandHandlerTests()
	{
		handler = new ResendVerificationEmailCommandHandler(keycloakAdmin);
	}

	[Fact]
	public async Task HandleAsync_ValidEmail_ReturnsSuccessAndSendsEmail()
	{
		var command = new ResendVerificationEmailCommand(TestEmail);
		var keycloakUserId = KeycloakUserId.New();

		keycloakAdmin
			.FindUserByEmailAsync(command.Email, Arg.Any<CancellationToken>())
			.Returns(Result<KeycloakUserId>.Success(keycloakUserId));

		keycloakAdmin
			.SendVerificationEmailAsync(keycloakUserId, Arg.Any<CancellationToken>())
			.Returns(Result.Success());

		var result = await handler.HandleAsync(command);

		result.IsSuccess.Should().BeTrue();

		await keycloakAdmin.Received(1).SendVerificationEmailAsync(
			keycloakUserId,
			Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task HandleAsync_UserNotFound_ReturnsSuccessToPreventEnumeration()
	{
		var command = new ResendVerificationEmailCommand(UnknownEmail);

		keycloakAdmin
			.FindUserByEmailAsync(command.Email, Arg.Any<CancellationToken>())
			.Returns(Result<KeycloakUserId>.Failure(VerificationErrors.UserNotFound));

		var result = await handler.HandleAsync(command);

		result.IsSuccess.Should().BeTrue();
	}

	[Fact]
	public async Task HandleAsync_UserNotFound_DoesNotAttemptToSendEmail()
	{
		var command = new ResendVerificationEmailCommand(UnknownEmail);

		keycloakAdmin
			.FindUserByEmailAsync(command.Email, Arg.Any<CancellationToken>())
			.Returns(Result<KeycloakUserId>.Failure(VerificationErrors.UserNotFound));

		await handler.HandleAsync(command);

		await keycloakAdmin.DidNotReceive().SendVerificationEmailAsync(
			Arg.Any<KeycloakUserId>(),
			Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task HandleAsync_IdentityProviderUnavailable_ReturnsFailure()
	{
		var command = new ResendVerificationEmailCommand(TestEmail);

		keycloakAdmin
			.FindUserByEmailAsync(command.Email, Arg.Any<CancellationToken>())
			.Returns(Result<KeycloakUserId>.Failure(VerificationErrors.IdentityProviderUnavailable));

		var result = await handler.HandleAsync(command);

		result.IsFailure.Should().BeTrue();
		result.Error.Should().Be(VerificationErrors.IdentityProviderUnavailable);
	}

	[Fact]
	public async Task HandleAsync_IdentityProviderUnavailable_DoesNotAttemptToSendEmail()
	{
		var command = new ResendVerificationEmailCommand(TestEmail);

		keycloakAdmin
			.FindUserByEmailAsync(command.Email, Arg.Any<CancellationToken>())
			.Returns(Result<KeycloakUserId>.Failure(VerificationErrors.IdentityProviderUnavailable));

		await handler.HandleAsync(command);

		await keycloakAdmin.DidNotReceive().SendVerificationEmailAsync(
			Arg.Any<KeycloakUserId>(),
			Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task HandleAsync_SendFailed_ReturnsFailure()
	{
		var command = new ResendVerificationEmailCommand(TestEmail);
		var keycloakUserId = KeycloakUserId.New();

		keycloakAdmin
			.FindUserByEmailAsync(command.Email, Arg.Any<CancellationToken>())
			.Returns(Result<KeycloakUserId>.Success(keycloakUserId));

		keycloakAdmin
			.SendVerificationEmailAsync(keycloakUserId, Arg.Any<CancellationToken>())
			.Returns(Result.Failure(VerificationErrors.SendFailed));

		var result = await handler.HandleAsync(command);

		result.IsFailure.Should().BeTrue();
		result.Error.Should().Be(VerificationErrors.SendFailed);
	}
}
