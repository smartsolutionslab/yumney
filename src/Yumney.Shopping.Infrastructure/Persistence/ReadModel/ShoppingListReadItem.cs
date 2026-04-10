namespace SmartSolutionsLab.Yumney.Shopping.Infrastructure.Persistence.ReadModel;

/// <summary>
/// Materialized read model for a single shopping list item.
/// Updated asynchronously by projection handlers.
/// </summary>
public sealed class ShoppingListReadItem
{
    public Guid Id { get; set; }

    public string OwnerId { get; set; } = default!;

    public string ItemName { get; set; } = default!;

    public decimal TotalQuantity { get; set; }

    public string? Unit { get; set; }

    public string Category { get; set; } = "other";

    public bool IsBought { get; set; }

    /// <summary>
    /// JSON-serialized list of sources (source label + quantity + timestamp).
    /// </summary>
    public string SourcesJson { get; set; } = "[]";

    public DateTime LastUpdated { get; set; }
}
