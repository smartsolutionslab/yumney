using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using SmartSolutionsLab.Yumney.Shared.Common;

namespace SmartSolutionsLab.Yumney.Recipes.Infrastructure.Services;

/// <summary>
/// HTTP-backed <see cref="IIngredientBalanceProvider"/> for callers in the
/// Recipes module. Calls the Shopping API balance endpoint and projects the
/// returned items to a name → freshness map used for case-insensitive
/// matching plus perishable-first ranking.
/// </summary>
public sealed class HttpIngredientBalanceProvider(IHttpClientFactory httpClientFactory) : IIngredientBalanceProvider
{
#pragma warning disable SA1311
	private static readonly JsonSerializerOptions jsonOptions = new(JsonSerializerDefaults.Web)
	{
		Converters = { new JsonStringEnumConverter() },
	};
#pragma warning restore SA1311

	public async Task<IReadOnlyDictionary<string, Freshness>> GetAvailableIngredientsAsync(CancellationToken cancellationToken = default)
	{
		var client = httpClientFactory.CreateClient("shopping-api");
		var dto = await client.GetFromJsonAsync<BalanceDto>("/api/v1/shopping-lists/balance", jsonOptions, cancellationToken);

		var result = new Dictionary<string, Freshness>(StringComparer.OrdinalIgnoreCase);
		if (dto?.Items is null) return result;

		foreach (var item in dto.Items)
		{
			var name = item.ItemName.Trim();
			if (name.Length == 0) continue;

			// Last write wins. Items with the same name but different units
			// share a freshness slot — the most "urgent" wins so the matcher
			// is not misled by a fresher duplicate.
			if (!result.TryGetValue(name, out var existing) || Compare(item.Freshness, existing) > 0)
			{
				result[name] = item.Freshness;
			}
		}

		return result;
	}

	private static int Compare(Freshness a, Freshness b) => Urgency(a).CompareTo(Urgency(b));

	private static int Urgency(Freshness freshness) => freshness switch
	{
		Freshness.CheckIt => 3,
		Freshness.UseSoon => 2,
		Freshness.Fresh => 1,
		_ => 0,
	};

#pragma warning disable SA1313, SA1402, SA1649
	private sealed record BalanceDto(IReadOnlyList<BalanceItemDto> Items);

	private sealed record BalanceItemDto(string ItemName, Freshness Freshness);
}
