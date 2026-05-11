using SmartSolutionsLab.Yumney.Recipes.Application.Interfaces;
using ShoppingClient = SmartSolutionsLab.Yumney.Shopping.Client;

namespace SmartSolutionsLab.Yumney.Recipes.Infrastructure.ExternalServices;

#pragma warning disable SA1303
public sealed class HttpShoppingListItemAdder(ShoppingClient.IShoppingClient shopping) : IShoppingListItemAdder
{
	private const string chatSourceLabel = "chat";

	public async Task<bool> AddAsync(AddShoppingItemRequest request, CancellationToken cancellationToken = default)
	{
		try
		{
			await shopping.AddItemAsync(
				new ShoppingClient.AddShoppingItemRequest(request.Name, request.Quantity ?? 1m, request.Unit, chatSourceLabel),
				cancellationToken);
			return true;
		}
		catch (Exception ex) when (ex is not OperationCanceledException)
		{
			return false;
		}
	}
}
