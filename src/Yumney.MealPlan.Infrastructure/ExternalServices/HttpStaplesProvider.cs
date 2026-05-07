using SmartSolutionsLab.Yumney.MealPlan.Application.Interfaces;
using SmartSolutionsLab.Yumney.Users.Client;

namespace SmartSolutionsLab.Yumney.MealPlan.Infrastructure.ExternalServices;

public sealed class HttpStaplesProvider(IUsersClient users) : IStaplesProvider
{
	public async Task<IReadOnlySet<string>> GetStapleNamesAsync(CancellationToken cancellationToken = default)
	{
		var staples = await users.GetMyStaplesAsync(cancellationToken);
		return staples.ToHashSet(StringComparer.OrdinalIgnoreCase);
	}
}
