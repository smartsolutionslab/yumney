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

    public Amount OnList { get; set; } = Amount.From(0);

    public Amount Bought { get; set; } = Amount.From(0);

    public Amount Consumed { get; set; } = Amount.From(0);

    public Amount Removed { get; set; } = Amount.From(0);

    public Amount AtHome => Amount.From(Math.Max(0, Bought.Value - Consumed.Value - Removed.Value));

    public decimal Remaining => OnList.Value - Bought.Value;

    public bool IsBought => Bought.Value > 0;

    /// <summary>
    /// Gets the grouping key: item name + unit (case-insensitive).
    /// </summary>
    public string GroupKey => $"{ItemName.Value.ToLowerInvariant()}|{Unit?.Value ?? string.Empty}";
}
