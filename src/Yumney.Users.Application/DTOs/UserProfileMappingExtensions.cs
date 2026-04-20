using SmartSolutionsLab.Yumney.Users.Domain.AppUserProfile;

namespace SmartSolutionsLab.Yumney.Users.Application.DTOs;

public static class UserProfileMappingExtensions
{
	extension(AppUserProfile profile)
	{
		public UserProfileDto ToDto()
		{
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
}
