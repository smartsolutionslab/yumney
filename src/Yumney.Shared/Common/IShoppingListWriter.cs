namespace SmartSolutionsLab.Yumney.Shared.Common;

/// <summary>
/// Cross-module interface for writing items to the shopping ledger.
/// Implemented by Shopping.Infrastructure, consumed by MealPlan.Application.
/// </summary>
public interface IShoppingListWriter
{
    Task AddItemsAsync(
        string ownerId,
        IReadOnlyList<ShoppingItemRequest> items,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// A single item to add to the shopping list.
/// </summary>
public sealed record ShoppingItemRequest(string ItemName, decimal Quantity, string? Unit, string Source);
