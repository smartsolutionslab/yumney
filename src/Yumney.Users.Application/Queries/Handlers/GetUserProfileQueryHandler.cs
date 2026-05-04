using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shared.CQRS;
using SmartSolutionsLab.Yumney.Shared.Outcomes;
using SmartSolutionsLab.Yumney.Users.Application.DTOs;
using SmartSolutionsLab.Yumney.Users.Domain.AppUserProfile;

namespace SmartSolutionsLab.Yumney.Users.Application.Queries.Handlers;

public sealed class GetUserProfileQueryHandler(IAppUserProfileRepository profiles, ICurrentUser currentUser)
	: IQueryHandler<GetUserProfileQuery, Result<UserProfileDto>>
{
	/// <inheritdoc />
	public async Task<Result<UserProfileDto>> HandleAsync(GetUserProfileQuery query, CancellationToken cancellationToken = default)
	{
		var keycloakId = KeycloakUserId.From(currentUser.UserId);
		var profile = await profiles.FindByKeycloakUserIdAsync(keycloakId, cancellationToken);

		if (profile is null) return new ApiError("PROFILE_NOT_FOUND", "User profile not found.", 404);

		return profile.ToDto();
	}
}
