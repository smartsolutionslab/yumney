using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingList;

namespace SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingLedger;

/// <summary>
/// Internal mutable state for a single item in the shopping list.
/// Rebuilt from events during aggregate hydration.
/// </summary>
#pragma warning disable SA1600
public sealed class ShoppingItemState
{
    public ItemName ItemName { get; init; } = default!;

    public Unit? Unit { get; init; }

    public decimal OnList { get; set; }

    public decimal Bought { get; set; }

    public decimal Consumed { get; set; }

    public decimal Removed { get; set; }

    public decimal AtHome => Math.Max(0, Bought - Consumed - Removed);

    public decimal Remaining => OnList - Bought;

    public bool IsBought => Bought > 0;

    /// <summary>
    /// Gets the grouping key: item name + unit (case-insensitive).
    /// </summary>
    public string GroupKey => $"{ItemName.Value.ToLowerInvariant()}|{Unit?.Value ?? string.Empty}";
}
