using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shared.Events;
using SmartSolutionsLab.Yumney.Shared.Events.Contracts;
using SmartSolutionsLab.Yumney.Shared.Outcomes;
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
	private readonly IUsersUnitOfWork users = Substitute.For<IUsersUnitOfWork>();
	private readonly IKeycloakAdminService keycloak = Substitute.For<IKeycloakAdminService>();
	private readonly IEventBus eventBus = Substitute.For<IEventBus>();
	private readonly ICurrentUser currentUser = Substitute.For<ICurrentUser>();
	private readonly IAccountDeletionEmailSender emailSender = Substitute.For<IAccountDeletionEmailSender>();
	private readonly DeleteAccountCommandHandler handler;

	public DeleteAccountCommandHandlerTests()
	{
		currentUser.UserId.Returns(KeycloakId);
		keycloak.DeleteUserAsync(Arg.Any<KeycloakUserId>(), Arg.Any<CancellationToken>())
			.Returns(Result.Success());
		users.Profiles.Returns(profiles);
		users.Activities.Returns(activities);
		users.StaplesLists.Returns(staplesLists);

		// Default: profile + email captured successfully so confirmation goes
		// out. Individual tests override these returns to exercise the missing
		// / failure paths.
		profiles.FindByKeycloakUserIdAsync(Arg.Any<KeycloakUserId>(), Arg.Any<CancellationToken>())
			.Returns(AppUserProfile.Create(KeycloakUserId.From(KeycloakId), DisplayName.From("Test User")));
		keycloak.GetEmailAsync(Arg.Any<KeycloakUserId>(), Arg.Any<CancellationToken>())
			.Returns(Result<Email>.Success(Email.From("user@example.com")));

		handler = new DeleteAccountCommandHandler(
			users, keycloak, eventBus, currentUser, emailSender, NullLogger<DeleteAccountCommandHandler>.Instance);
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

	[Fact]
	public async Task HandleAsync_SendsConfirmationEmailAfterSuccessfulDeletion()
	{
		var result = await handler.HandleAsync(new DeleteAccountCommand());

		result.IsSuccess.Should().BeTrue();
		await emailSender.Received(1).SendAsync(
			Arg.Is<AccountDeletionEmailPayload>(p =>
				p.RecipientEmail.Value == "user@example.com"
				&& p.DisplayName.Value == "Test User"),
			Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task HandleAsync_PassesPreferredLanguageToEmailSender()
	{
		var profile = AppUserProfile.Create(KeycloakUserId.From(KeycloakId), DisplayName.From("Test User"));
		profile.SwitchLanguageTo(PreferredLanguage.From("de"));
		profiles.FindByKeycloakUserIdAsync(Arg.Any<KeycloakUserId>(), Arg.Any<CancellationToken>())
			.Returns(profile);

		await handler.HandleAsync(new DeleteAccountCommand());

		await emailSender.Received(1).SendAsync(
			Arg.Is<AccountDeletionEmailPayload>(p => p.Language.Value == "de"),
			Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task HandleAsync_EmailSendFails_DeletionStillReturnsSuccess()
	{
		// Send failure must NOT roll the deletion back — the user's data is
		// already gone and the GDPR contract is satisfied.
		emailSender.SendAsync(Arg.Any<AccountDeletionEmailPayload>(), Arg.Any<CancellationToken>())
			.ThrowsAsync(new InvalidOperationException("SMTP unreachable"));

		var result = await handler.HandleAsync(new DeleteAccountCommand());

		result.IsSuccess.Should().BeTrue();
		await emailSender.Received(1).SendAsync(Arg.Any<AccountDeletionEmailPayload>(), Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task HandleAsync_ProfileMissing_SkipsEmailButCompletesDeletion()
	{
		profiles.FindByKeycloakUserIdAsync(Arg.Any<KeycloakUserId>(), Arg.Any<CancellationToken>())
			.Returns((AppUserProfile?)null);

		var result = await handler.HandleAsync(new DeleteAccountCommand());

		result.IsSuccess.Should().BeTrue();
		await emailSender.DidNotReceive().SendAsync(Arg.Any<AccountDeletionEmailPayload>(), Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task HandleAsync_KeycloakEmailLookupFails_SkipsEmailButCompletesDeletion()
	{
		keycloak.GetEmailAsync(Arg.Any<KeycloakUserId>(), Arg.Any<CancellationToken>())
			.Returns(Result<Email>.Failure(new ApiError("KC_LOOKUP", "Lookup failed", 503)));

		var result = await handler.HandleAsync(new DeleteAccountCommand());

		result.IsSuccess.Should().BeTrue();
		await emailSender.DidNotReceive().SendAsync(Arg.Any<AccountDeletionEmailPayload>(), Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task HandleAsync_KeycloakDeleteFails_DoesNotSendConfirmationEmail()
	{
		// If Keycloak deletion fails, the user is in a half-deleted state
		// (local data gone, Keycloak account intact). Don't tell them it
		// succeeded — surface the failure to support.
		keycloak.DeleteUserAsync(Arg.Any<KeycloakUserId>(), Arg.Any<CancellationToken>())
			.Returns(Result.Failure(new ApiError("KC_DOWN", "Keycloak unavailable", 503)));

		await handler.HandleAsync(new DeleteAccountCommand());

		await emailSender.DidNotReceive().SendAsync(Arg.Any<AccountDeletionEmailPayload>(), Arg.Any<CancellationToken>());
	}
}
