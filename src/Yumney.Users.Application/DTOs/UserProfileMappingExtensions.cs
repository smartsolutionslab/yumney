using SmartSolutionsLab.Yumney.Users.Domain.AppUserProfile;

namespace SmartSolutionsLab.Yumney.Users.Application.DTOs;

public static class UserProfileMappingExtensions
{
	public static UserProfileDto ToDto(this AppUserProfile profile) =>
		new(
			profile.DisplayName.Value,
			profile.PreferredLanguage.Value,
			profile.PreferredUnitSystem.Value,
			profile.DefaultServings.Value,
			profile.DietaryProfile.ToDto());

	public static DietaryProfileDto ToDto(this DietaryProfile dietary) =>
		new(
			dietary.DietaryType?.Value,
			dietary.Restrictions.Select(restriction => restriction.Value).ToList(),
			dietary.BalanceGoals.MinVeggieMeals,
			dietary.BalanceGoals.MaxRedMeatMeals,
			dietary.CookingEffort?.Value);
}
