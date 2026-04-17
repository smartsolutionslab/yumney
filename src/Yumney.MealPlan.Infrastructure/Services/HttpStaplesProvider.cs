using System.Net.Http.Json;
using SmartSolutionsLab.Yumney.Shared.Common;

namespace SmartSolutionsLab.Yumney.MealPlan.Infrastructure.Services;

public sealed class HttpStaplesProvider(IHttpClientFactory httpClientFactory) : IStaplesProvider
{
    public async Task<IReadOnlySet<string>> GetStapleNamesAsync(
        string ownerId,
        CancellationToken cancellationToken = default)
    {
        var client = httpClientFactory.CreateClient("users-api");
        var staples = await client.GetFromJsonAsync<List<string>>(
            "/api/v1/users/staples",
            cancellationToken);

        return (staples ?? []).ToHashSet(StringComparer.OrdinalIgnoreCase);
    }
}
