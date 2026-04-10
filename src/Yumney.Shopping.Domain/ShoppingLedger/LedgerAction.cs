namespace SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingLedger;

/// <summary>
/// Actions that can occur on a shopping ledger item.
/// </summary>
public enum LedgerAction
{
    /// <summary>Item added to shopping list.</summary>
    Added,

    /// <summary>Item marked as bought/purchased.</summary>
    Bought,

    /// <summary>Item consumed (used in cooking).</summary>
    Consumed,

    /// <summary>Item removed from tracking.</summary>
    Removed,

    /// <summary>Item quantity adjusted.</summary>
    Adjusted,

    /// <summary>Previous transaction rolled back.</summary>
    RolledBack,
}
