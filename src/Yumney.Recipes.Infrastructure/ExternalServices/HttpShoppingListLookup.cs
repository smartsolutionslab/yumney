using SmartSolutionsLab.Yumney.Recipes.Application.Interfaces;
using SmartSolutionsLab.Yumney.Shopping.Client;

namespace SmartSolutionsLab.Yumney.Recipes.Infrastructure.ExternalServices;

public sealed class HttpShoppingListLookup(IShoppingClient shopping) : IShoppingListLookup
{
	public async Task<ShoppingListLookupResult?> GetMergedAsync(bool includePastBought = false, CancellationToken cancellationToken = default)
	{
		var response = await shopping.GetMergedListAsync(includePastBought, cancellationToken);
		return response?.ToLookupResult();
	}
}
