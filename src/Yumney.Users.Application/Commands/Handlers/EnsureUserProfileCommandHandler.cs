using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shared.CQRS;
using SmartSolutionsLab.Yumney.Shared.Outcomes;
using SmartSolutionsLab.Yumney.Users.Domain.AppUserProfile;

namespace SmartSolutionsLab.Yumney.Users.Application.Commands.Handlers;

public sealed class EnsureUserProfileCommandHandler(IUsersUnitOfWork unitOfWork, ICurrentUser currentUser)
	: ICommandHandler<EnsureUserProfileCommand, Result>
{
	public async Task<Result> HandleAsync(EnsureUserProfileCommand command, CancellationToken cancellationToken = default)
	{
		var keycloakId = KeycloakUserId.From(currentUser.UserId);
		var existing = await unitOfWork.Profiles.FindByKeycloakUserIdAsync(keycloakId, cancellationToken);
		if (existing is not null) return Result.Success();

		// JIT-provision from JWT claims. Users authenticated via Keycloak
		// (realm-seeded test users, SSO providers, admin-created users) may
		// not yet have an AppUserProfile row. Create one on first profile
		// request so the app doesn't block on 404.
		var displayName = DisplayName.From(
			string.IsNullOrWhiteSpace(currentUser.DisplayName) ? currentUser.Email : currentUser.DisplayName);
		var profile = AppUserProfile.Create(keycloakId, displayName);
		await unitOfWork.Profiles.AddAsync(profile, cancellationToken);
		await unitOfWork.SaveChangesAsync(cancellationToken);
		return Result.Success();
	}
}
