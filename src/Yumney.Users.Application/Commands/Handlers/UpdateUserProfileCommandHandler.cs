using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shared.CQRS;
using SmartSolutionsLab.Yumney.Users.Application.DTOs;
using SmartSolutionsLab.Yumney.Users.Domain.AppUserProfile;

namespace SmartSolutionsLab.Yumney.Users.Application.Commands.Handlers;

#pragma warning disable SA1601
public sealed partial class UpdateUserProfileCommandHandler(
    IAppUserProfileRepository profiles,
    ICurrentUser currentUser) : ICommandHandler<UpdateUserProfileCommand, Result<UserProfileDto>>
{
    /// <inheritdoc />
    public async Task<Result<UserProfileDto>> HandleAsync(UpdateUserProfileCommand command, CancellationToken cancellationToken = default)
    {
        var (defaultServings, dietaryTypeValue, restrictionValues, minVeggieMeals, maxRedMeatMeals, cookingEffortValue) = command;


        var keycloakId = KeycloakUserId.From(currentUser.UserId);
        var profile = await profiles.GetByKeycloakUserIdAsync(keycloakId, cancellationToken);

        profile.AdjustDefaultServingsTo(DefaultServings.From(defaultServings));

        var dietaryType = dietaryTypeValue is not null ? DietaryType.From(dietaryTypeValue) : null;
        var restrictions = restrictionValues.Select(DietaryRestriction.From).ToList();
        var balanceGoals = WeeklyBalanceGoals.From(minVeggieMeals, maxRedMeatMeals);
        var cookingEffort = cookingEffortValue is not null ? CookingEffortPreference.From(cookingEffortValue) : null;

        profile.UpdateDietaryProfile(DietaryProfile.From(dietaryType, restrictions, balanceGoals, cookingEffort));

        await profiles.SaveChangesAsync(cancellationToken);

        var dietary = profile.DietaryProfile;
        return new UserProfileDto(
            profile.DisplayName.Value,
            profile.PreferredLanguage.Value,
            profile.PreferredUnitSystem.Value,
            profile.DefaultServings.Value,
            new DietaryProfileDto(
                dietary.DietaryType?.Value,
                dietary.Restrictions.Select(r => r.Value).ToList(),
                dietary.BalanceGoals.MinVeggieMeals,
                dietary.BalanceGoals.MaxRedMeatMeals,
                dietary.CookingEffort?.Value));
    }
}
