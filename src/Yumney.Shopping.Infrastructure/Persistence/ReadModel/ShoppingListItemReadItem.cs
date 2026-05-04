namespace SmartSolutionsLab.Yumney.Shopping.Infrastructure.Persistence.ReadModel;

/// <summary>
/// Materialized read model for one item within a ShoppingList. Populated by
/// <see cref="ShoppingListProjection"/> from the list event stream.
/// </summary>
public sealed class ShoppingListItemReadItem
{
	public Guid Id { get; set; }

	public Guid ListId { get; set; }

	public string OwnerId { get; set; } = default!;

	public string Name { get; set; } = default!;

	public decimal? QuantityAmount { get; set; }

	public string? QuantityUnit { get; set; }

	public string Category { get; set; } = "other";

	public bool IsChecked { get; set; }

	public DateTime CreatedAt { get; set; }

	public DateTime LastUpdated { get; set; }
}
