using SmartSolutionsLab.Yumney.Shared.Quantities;

namespace SmartSolutionsLab.Yumney.Shopping.Client;

public interface IShoppingClient
{
	Task<ShoppingBalanceResponse?> GetBalanceAsync(CancellationToken cancellationToken = default);

	Task AddItemAsync(AddShoppingItemRequest request, CancellationToken cancellationToken = default);
}

public sealed record ShoppingBalanceResponse(IReadOnlyList<ShoppingBalanceItem> Items);

public sealed record ShoppingBalanceItem(string ItemName, Freshness Freshness);

public sealed record AddShoppingItemRequest(string Name, decimal Quantity, string? Unit, string Source);
