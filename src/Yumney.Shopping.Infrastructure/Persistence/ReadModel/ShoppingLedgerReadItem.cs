using SmartSolutionsLab.Yumney.Shared.Common;

namespace SmartSolutionsLab.Yumney.Shopping.Infrastructure.Persistence.ReadModel;

/// <summary>
/// Materialized read model for a single shopping list item.
/// Updated asynchronously by projection handlers.
/// </summary>
public sealed class ShoppingLedgerReadItem
{
	public Guid Id { get; set; }

	public string OwnerId { get; set; } = default!;

	public string ItemName { get; set; } = default!;

	public decimal TotalQuantity { get; set; }

	public string? Unit { get; set; }

	public string Category { get; set; } = IngredientCategory.Other.Value;

	public bool IsBought { get; set; }

	public DateTime? BoughtAt { get; set; }

	/// <summary>
	/// Gets or sets the JSON-serialized list of sources (source label + quantity + timestamp).
	/// </summary>
	public string SourcesJson { get; set; } = "[]";

	public DateTime LastUpdated { get; set; }
}
