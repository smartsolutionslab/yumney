using SmartSolutionsLab.Yumney.Recipes.Application.Interfaces;
using SmartSolutionsLab.Yumney.Shopping.Client;

namespace SmartSolutionsLab.Yumney.Recipes.Infrastructure.ExternalServices;

public sealed class HttpShoppingListItemRemover(IShoppingClient shopping) : IShoppingListItemRemover
{
	public Task<bool> RemoveAsync(RemoveShoppingItemRequest request, CancellationToken cancellationToken = default) =>
		shopping.RemoveItemAsync(
			new RemoveShoppingItemBody(request.Name, request.Quantity, request.Unit, request.Reason),
			cancellationToken);
}
