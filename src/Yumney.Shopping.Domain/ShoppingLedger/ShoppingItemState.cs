namespace SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingLedger;

/// <summary>
/// Internal mutable state for a single item in the shopping list.
/// Rebuilt from events during aggregate hydration.
/// </summary>
#pragma warning disable SA1600
public sealed class ShoppingItemState
{
    public string ItemName { get; init; } = default!;

    public string? Unit { get; init; }

    public decimal OnList { get; set; }

    public decimal Bought { get; set; }

    public decimal Consumed { get; set; }

    public decimal Removed { get; set; }

    public decimal AtHome => Math.Max(0, Bought - Consumed - Removed);

    public decimal Remaining => OnList - Bought;

    public bool IsBought => Bought > 0;

    /// <summary>
    /// Key used for grouping: item name + unit (case-insensitive).
    /// </summary>
    public string GroupKey => $"{ItemName.ToLowerInvariant()}|{Unit ?? string.Empty}";
}
