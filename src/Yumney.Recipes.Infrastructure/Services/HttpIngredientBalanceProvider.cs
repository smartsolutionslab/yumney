using System.Net.Http.Json;
using SmartSolutionsLab.Yumney.Shared.Common;

namespace SmartSolutionsLab.Yumney.Recipes.Infrastructure.Services;

/// <summary>
/// HTTP-backed <see cref="IIngredientBalanceProvider"/> for callers in the
/// Recipes module. Calls the Shopping API balance endpoint and projects the
/// returned items to a lowercased name set used for case-insensitive matching.
/// </summary>
public sealed class HttpIngredientBalanceProvider(IHttpClientFactory httpClientFactory) : IIngredientBalanceProvider
{
	public async Task<IReadOnlySet<string>> GetAvailableIngredientNamesAsync(string ownerId, CancellationToken cancellationToken = default)
	{
		var client = httpClientFactory.CreateClient("shopping-api");
		var dto = await client.GetFromJsonAsync<BalanceDto>("/api/v1/shopping-lists/balance", cancellationToken);

		if (dto?.Items is null)
		{
			return new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		}

		return dto.Items
			.Select(i => i.ItemName.Trim())
			.Where(n => n.Length > 0)
			.ToHashSet(StringComparer.OrdinalIgnoreCase);
	}

#pragma warning disable SA1313, SA1402, SA1649
	private sealed record BalanceDto(IReadOnlyList<BalanceItemDto> Items);

	private sealed record BalanceItemDto(string ItemName);
}
