using SmartSolutionsLab.Yumney.MealPlan.Application.Interfaces;
using SmartSolutionsLab.Yumney.Users.Client;

namespace SmartSolutionsLab.Yumney.MealPlan.Infrastructure.ExternalServices;

public sealed class HttpDietaryProfileProvider(IUsersClient users) : IDietaryProfileProvider
{
	public async Task<DietaryProfileSnapshot> GetAsync(CancellationToken cancellationToken = default)
	{
		var profile = await users.GetMyProfileAsync(cancellationToken);
		var dietary = profile?.DietaryProfile;
		if (dietary is null) return DietaryProfileSnapshot.Empty;
		return new DietaryProfileSnapshot(dietary.DietaryType, dietary.Restrictions ?? []);
	}
}
