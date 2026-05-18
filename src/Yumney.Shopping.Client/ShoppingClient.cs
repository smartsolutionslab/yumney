using SmartSolutionsLab.Yumney.Shared.Web;

namespace SmartSolutionsLab.Yumney.Shopping.Client;

internal sealed class ShoppingClient(IModuleHttpClientFactory factory) : IShoppingClient
{
	private readonly IModuleHttpClient http = factory.For("shopping-api");

	public Task<ShoppingBalanceResponse?> GetBalanceAsync(CancellationToken cancellationToken = default) =>
		http.FindAsync<ShoppingBalanceResponse>("/api/v1/shopping-lists/balance", "GetShoppingBalance", cancellationToken);

	public Task AddItemAsync(AddShoppingItemRequest request, CancellationToken cancellationToken = default) =>
		http.PostAsync("/api/v1/shopping-lists/items", request, "AddShoppingItem", cancellationToken);

	public Task<MergedShoppingListResponse?> GetMergedListAsync(bool includePastBought = false, CancellationToken cancellationToken = default) =>
		http.FindAsync<MergedShoppingListResponse>(
			$"/api/v1/shopping-lists/merged?includePastBought={includePastBought.ToString().ToLowerInvariant()}",
			"GetMergedShoppingList",
			cancellationToken);

	public async Task<bool> CreateListFromRecipesAsync(CreateListFromRecipesBody body, CancellationToken cancellationToken = default)
	{
		try
		{
			await http.PostAsync("/api/v1/shopping-lists/from-recipes", body, "CreateShoppingListFromRecipes", cancellationToken);
			return true;
		}
		catch (Exception ex) when (ex is not OperationCanceledException)
		{
			return false;
		}
	}

	public async Task<bool> RemoveItemAsync(RemoveShoppingItemBody body, CancellationToken cancellationToken = default)
	{
		try
		{
			await http.DeleteAsync("/api/v1/shopping-lists/items", body, "RemoveShoppingItem", cancellationToken);
			return true;
		}
		catch (Exception ex) when (ex is not OperationCanceledException)
		{
			return false;
		}
	}
}
