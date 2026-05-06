using Microsoft.Extensions.Logging;
using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shared.CQRS;
using SmartSolutionsLab.Yumney.Shared.Events;
using SmartSolutionsLab.Yumney.Shared.Events.CrossModule;
using SmartSolutionsLab.Yumney.Shared.Outcomes;
using SmartSolutionsLab.Yumney.Users.Application.Interfaces;
using SmartSolutionsLab.Yumney.Users.Domain.AppUserProfile;
using ActivityOwner = SmartSolutionsLab.Yumney.Users.Domain.UserActivity.OwnerIdentifier;
using StaplesOwner = SmartSolutionsLab.Yumney.Users.Domain.StaplesList.OwnerIdentifier;

namespace SmartSolutionsLab.Yumney.Users.Application.Commands.Handlers;

#pragma warning disable SA1601
public sealed partial class DeleteAccountCommandHandler(
	IUsersUnitOfWork unitOfWork,
	IKeycloakAdminService keycloak,
	IEventBus eventBus,
	ICurrentUser currentUser,
	ILogger<DeleteAccountCommandHandler> logger)
	: ICommandHandler<DeleteAccountCommand, Result>
{
	public async Task<Result> HandleAsync(DeleteAccountCommand command, CancellationToken cancellationToken = default)
	{
		var keycloakId = KeycloakUserId.From(currentUser.UserId);
		LogDeletionRequested(keycloakId.Value);

		// 1. Tell every other module to wipe owner-scoped data first. Subscribers
		// must be idempotent — see UserAccountDeletedIntegrationEvent docstring.
		await eventBus.PublishAsync(new UserAccountDeletedIntegrationEvent(keycloakId.Value), cancellationToken);

		// 2. Erase Users-module-owned data (profile, activity, staples). The repos
		// stage the deletes via the change tracker; SaveChangesAsync commits them
		// in a single transaction so a partial failure rolls back automatically.
		await unitOfWork.Activities.DeleteAllByOwnerAsync(ActivityOwner.From(keycloakId.Value), cancellationToken);
		await unitOfWork.StaplesLists.DeleteByOwnerAsync(StaplesOwner.From(keycloakId.Value), cancellationToken);
		await unitOfWork.Profiles.DeleteByKeycloakUserIdAsync(keycloakId, cancellationToken);
		await unitOfWork.SaveChangesAsync(cancellationToken);

		// 3. Finally, drop the Keycloak account. If this fails the user is locked
		// out (no profile, no Keycloak user) but their PII is already gone, which
		// satisfies GDPR — surface the failure so support can finish manually.
		var keycloakResult = await keycloak.DeleteUserAsync(keycloakId, cancellationToken);
		if (keycloakResult.IsFailure)
		{
			LogKeycloakDeletionFailed(keycloakId.Value);
			return Result.Failure(DeleteAccountErrors.IdentityProviderUnavailable);
		}

		LogDeletionCompleted(keycloakId.Value);
		return Result.Success();
	}

	[LoggerMessage(Level = LogLevel.Information, Message = "GDPR: account deletion requested for {KeycloakUserId}")]
	private partial void LogDeletionRequested(string keycloakUserId);

	[LoggerMessage(Level = LogLevel.Information, Message = "GDPR: account deletion completed for {KeycloakUserId}")]
	private partial void LogDeletionCompleted(string keycloakUserId);

	[LoggerMessage(Level = LogLevel.Error, Message = "GDPR: local data erased but Keycloak deletion failed for {KeycloakUserId}")]
	private partial void LogKeycloakDeletionFailed(string keycloakUserId);
}
