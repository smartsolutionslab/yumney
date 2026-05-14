using SmartSolutionsLab.Yumney.Recipes.Application.Interfaces;
using SmartSolutionsLab.Yumney.Shared.Quantities;
using SmartSolutionsLab.Yumney.Shopping.Client;

namespace SmartSolutionsLab.Yumney.Recipes.Infrastructure.ExternalServices;

public sealed class HttpIngredientBalanceProvider(IShoppingClient shopping) : IIngredientBalanceProvider
{
	public async Task<IReadOnlyDictionary<string, Freshness>> GetAvailableIngredientsAsync(CancellationToken cancellationToken = default)
	{
		Dictionary<string, Freshness> result = new(StringComparer.OrdinalIgnoreCase);

		var balance = await shopping.GetBalanceAsync(cancellationToken);
		if (balance?.Items is null) return result;

		foreach (var item in balance.Items)
		{
			var name = item.ItemName.Trim();
			if (name.Length == 0) continue;

			if (!result.TryGetValue(name, out var existing) || item.Freshness.Urgency() > existing.Urgency())
			{
				result[name] = item.Freshness;
			}
		}

		return result;
	}
}
