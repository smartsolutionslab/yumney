using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shared.CQRS;
using SmartSolutionsLab.Yumney.Users.Application.DTOs;
using SmartSolutionsLab.Yumney.Users.Domain.AppUserProfile;

namespace SmartSolutionsLab.Yumney.Users.Application.Queries.Handlers;

public sealed class GetUserProfileQueryHandler(
    IAppUserProfileRepository profiles,
    ICurrentUser currentUser) : IQueryHandler<GetUserProfileQuery, Result<UserProfileDto>>
{
    /// <inheritdoc />
    public async Task<Result<UserProfileDto>> HandleAsync(GetUserProfileQuery query, CancellationToken cancellationToken = default)
    {
        var keycloakId = KeycloakUserId.From(currentUser.UserId);
        var profile = await profiles.FindByKeycloakUserIdAsync(keycloakId, cancellationToken);

        if (profile is null)
            return new ApiError("PROFILE_NOT_FOUND", "User profile not found.", 404);

        var dietary = profile.DietaryProfile;
        var dietaryDto = new DietaryProfileDto(
            dietary.DietaryType?.Value,
            dietary.Restrictions.Select(r => r.Value).ToList(),
            dietary.BalanceGoals.MinVeggieMeals,
            dietary.BalanceGoals.MaxRedMeatMeals,
            dietary.CookingEffort?.Value);

        return new UserProfileDto(
            profile.DisplayName.Value,
            profile.PreferredLanguage.Value,
            profile.PreferredUnitSystem.Value,
            profile.DefaultServings.Value,
            dietaryDto);
    }
}
