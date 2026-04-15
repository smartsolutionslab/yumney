namespace SmartSolutionsLab.Yumney.Shared.Common;

/// <summary>
/// A single item to add to the shopping list.
/// </summary>
public sealed record ShoppingItemRequest(string ItemName, decimal Quantity, string? Unit, string Source);
