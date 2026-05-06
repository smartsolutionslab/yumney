using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using SmartSolutionsLab.Yumney.Recipes.Application.Interfaces;
using SmartSolutionsLab.Yumney.Shared.Quantities;

namespace SmartSolutionsLab.Yumney.Recipes.Infrastructure.ExternalServices;

#pragma warning disable SA1601
public sealed partial class HttpIngredientBalanceProvider(
	IHttpClientFactory httpClientFactory,
	ILogger<HttpIngredientBalanceProvider> logger) : IIngredientBalanceProvider
{
#pragma warning disable SA1311
	private static readonly JsonSerializerOptions jsonOptions = new(JsonSerializerDefaults.Web)
	{
		Converters = { new JsonStringEnumConverter() },
	};
#pragma warning restore SA1311

	public async Task<IReadOnlyDictionary<string, Freshness>> GetAvailableIngredientsAsync(CancellationToken cancellationToken = default)
	{
		var result = new Dictionary<string, Freshness>(StringComparer.OrdinalIgnoreCase);

		BalanceDto? dto;
		try
		{
			var client = httpClientFactory.CreateClient("shopping-api");
			dto = await client.GetFromJsonAsync<BalanceDto>("/api/v1/shopping-lists/balance", jsonOptions, cancellationToken);
		}
		catch (Exception ex) when (ex is not OperationCanceledException)
		{
			LogBalanceFetchFailed(ex.Message);
			return result;
		}

		if (dto?.Items is null) return result;

		foreach (var item in dto.Items)
		{
			var name = item.ItemName.Trim();
			if (name.Length == 0) continue;

			// Items with the same name but different units share a freshness slot;
			// the most urgent wins so the matcher is not misled by a fresher duplicate.
			if (!result.TryGetValue(name, out var existing) || item.Freshness.Urgency() > existing.Urgency())
			{
				result[name] = item.Freshness;
			}
		}

		return result;
	}

	[LoggerMessage(Level = LogLevel.Warning, Message = "Failed to fetch ingredient balance from shopping-api ({Reason}); cookable-recipes matcher continuing with empty balance.")]
	private partial void LogBalanceFetchFailed(string reason);

#pragma warning disable SA1313, SA1402, SA1649
	private sealed record BalanceDto(IReadOnlyList<BalanceItemDto> Items);

	private sealed record BalanceItemDto(string ItemName, Freshness Freshness);
}
