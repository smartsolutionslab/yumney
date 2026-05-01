using System.Net.Http.Json;
using SmartSolutionsLab.Yumney.MealPlan.Application.Interfaces;
using SmartSolutionsLab.Yumney.MealPlan.Domain.WeeklyPlan;

namespace SmartSolutionsLab.Yumney.MealPlan.Infrastructure.ExternalServices;

public sealed class HttpStaplesProvider(IHttpClientFactory httpClientFactory) : IStaplesProvider
{
	public async Task<IReadOnlySet<string>> GetStapleNamesAsync(OwnerIdentifier owner, CancellationToken cancellationToken = default)
	{
		var client = httpClientFactory.CreateClient("users-api");
		List<string> staples = await client.GetFromJsonAsync<List<string>>("/api/v1/users/staples", cancellationToken) ?? [];

		return staples.ToHashSet(StringComparer.OrdinalIgnoreCase);
	}
}
