namespace SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingLedger;

/// <summary>
/// A merged view of shopping items — same item + same unit summed into one line
/// with source breakdown available for expansion.
/// </summary>
public sealed record MergedShoppingItem(
    string ItemName,
    decimal TotalQuantity,
    string? Unit,
    bool IsBought,
    IReadOnlyList<ItemSource> Sources);

/// <summary>
/// One contribution to a merged item — who added what amount and when.
/// </summary>
public sealed record ItemSource(
    decimal Quantity,
    string Source,
    DateTime OccurredAt);
