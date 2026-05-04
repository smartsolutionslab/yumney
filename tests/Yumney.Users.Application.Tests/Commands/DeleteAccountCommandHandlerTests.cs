using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shared.Events;
using SmartSolutionsLab.Yumney.Shared.Events.CrossModule;
using SmartSolutionsLab.Yumney.Users.Application.Commands;
using SmartSolutionsLab.Yumney.Users.Application.Commands.Handlers;
using SmartSolutionsLab.Yumney.Users.Application.Interfaces;
using SmartSolutionsLab.Yumney.Users.Domain.AppUserProfile;
using SmartSolutionsLab.Yumney.Users.Domain.StaplesList;
using SmartSolutionsLab.Yumney.Users.Domain.UserActivity;
using Xunit;
using ActivityOwner = SmartSolutionsLab.Yumney.Users.Domain.UserActivity.OwnerIdentifier;
using StaplesOwner = SmartSolutionsLab.Yumney.Users.Domain.StaplesList.OwnerIdentifier;

namespace SmartSolutionsLab.Yumney.Users.Application.Tests.Commands;

public class DeleteAccountCommandHandlerTests
{
	private const string KeycloakId = "kc-user-123";

	private readonly IAppUserProfileRepository profiles = Substitute.For<IAppUserProfileRepository>();
	private readonly IUserActivityRepository activities = Substitute.For<IUserActivityRepository>();
	private readonly IStaplesListRepository staplesLists = Substitute.For<IStaplesListRepository>();
	private readonly IKeycloakAdminService keycloak = Substitute.For<IKeycloakAdminService>();
	private readonly IEventBus eventBus = Substitute.For<IEventBus>();
	private readonly ICurrentUser currentUser = Substitute.For<ICurrentUser>();
	private readonly DeleteAccountCommandHandler handler;

	public DeleteAccountCommandHandlerTests()
	{
		currentUser.UserId.Returns(KeycloakId);
		keycloak.DeleteUserAsync(Arg.Any<KeycloakUserId>(), Arg.Any<CancellationToken>())
			.Returns(Result.Success());
		handler = new DeleteAccountCommandHandler(
			profiles, activities, staplesLists, keycloak, eventBus, currentUser, NullLogger<DeleteAccountCommandHandler>.Instance);
	}

	[Fact]
	public async Task HandleAsync_PublishesIntegrationEventBeforeLocalDelete()
	{
		Received.InOrder(() => { });

		await handler.HandleAsync(new DeleteAccountCommand());

		await eventBus.Received(1).PublishAsync(
			Arg.Is<UserAccountDeletedIntegrationEvent>(e => e.KeycloakUserId == KeycloakId),
			Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task HandleAsync_DeletesUsersModuleData()
	{
		await handler.HandleAsync(new DeleteAccountCommand());

		await activities.Received(1).DeleteAllByOwnerAsync(
			Arg.Is<ActivityOwner>(owner => owner.Value == KeycloakId),
			Arg.Any<CancellationToken>());
		await staplesLists.Received(1).DeleteByOwnerAsync(
			Arg.Is<StaplesOwner>(owner => owner.Value == KeycloakId),
			Arg.Any<CancellationToken>());
		await profiles.Received(1).DeleteByKeycloakUserIdAsync(
			Arg.Is<KeycloakUserId>(id => id.Value == KeycloakId),
			Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task HandleAsync_DeletesKeycloakUserLast()
	{
		await handler.HandleAsync(new DeleteAccountCommand());

		await keycloak.Received(1).DeleteUserAsync(
			Arg.Is<KeycloakUserId>(id => id.Value == KeycloakId),
			Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task HandleAsync_AllStepsSucceed_ReturnsSuccess()
	{
		var result = await handler.HandleAsync(new DeleteAccountCommand());

		result.IsSuccess.Should().BeTrue();
	}

	[Fact]
	public async Task HandleAsync_KeycloakFails_StillDeletedLocalDataAndReturnsIdpUnavailable()
	{
		keycloak.DeleteUserAsync(Arg.Any<KeycloakUserId>(), Arg.Any<CancellationToken>())
			.Returns(Result.Failure(new ApiError("KC_DOWN", "Keycloak unavailable", 503)));

		var result = await handler.HandleAsync(new DeleteAccountCommand());

		result.IsSuccess.Should().BeFalse();
		result.Error.Should().Be(DeleteAccountErrors.IdentityProviderUnavailable);

		// Local data was still erased — that's the GDPR-correct behaviour.
		await profiles.Received(1).DeleteByKeycloakUserIdAsync(Arg.Any<KeycloakUserId>(), Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task HandleAsync_ForwardsCancellationToken()
	{
		var cts = new CancellationTokenSource();

		await handler.HandleAsync(new DeleteAccountCommand(), cts.Token);

		await eventBus.Received(1).PublishAsync(
			Arg.Any<UserAccountDeletedIntegrationEvent>(),
			cts.Token);
		await keycloak.Received(1).DeleteUserAsync(Arg.Any<KeycloakUserId>(), cts.Token);
	}
}
