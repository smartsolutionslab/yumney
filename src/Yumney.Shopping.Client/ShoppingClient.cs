using SmartSolutionsLab.Yumney.Shared.Web;

namespace SmartSolutionsLab.Yumney.Shopping.Client;

internal sealed class ShoppingClient(IModuleHttpClientFactory factory) : IShoppingClient
{
	private readonly IModuleHttpClient http = factory.For("shopping-api");

	public Task<ShoppingBalanceResponse?> GetBalanceAsync(CancellationToken cancellationToken = default) =>
		http.FindAsync<ShoppingBalanceResponse>(
			"/api/v1/shopping-lists/balance",
			"GetShoppingBalance",
			cancellationToken);

	public Task AddItemAsync(AddShoppingItemRequest request, CancellationToken cancellationToken = default) =>
		http.PostAsync(
			"/api/v1/shopping-lists/items",
			request,
			"AddShoppingItem",
			cancellationToken);
}
