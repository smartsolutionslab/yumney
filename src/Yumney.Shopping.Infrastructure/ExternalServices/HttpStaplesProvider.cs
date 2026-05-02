using System.Net.Http.Json;
using SmartSolutionsLab.Yumney.Shopping.Application.Interfaces;
using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingList;

namespace SmartSolutionsLab.Yumney.Shopping.Infrastructure.ExternalServices;

public sealed class HttpStaplesProvider(IHttpClientFactory httpClientFactory) : IStaplesProvider
{
	public async Task<IReadOnlySet<string>> GetStapleNamesAsync(OwnerIdentifier owner, CancellationToken cancellationToken = default)
	{
		var client = httpClientFactory.CreateClient("users-api");
		var url = "/api/v1/users/staples";
		List<string> staples = await client.GetFromJsonAsync<List<string>>(url, cancellationToken) ?? [];

		return staples.ToHashSet(StringComparer.OrdinalIgnoreCase);
	}
}
