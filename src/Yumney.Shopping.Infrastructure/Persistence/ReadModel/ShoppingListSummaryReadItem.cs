namespace SmartSolutionsLab.Yumney.Shopping.Infrastructure.Persistence.ReadModel;

/// <summary>
/// Materialized read model for one ShoppingList aggregate. Populated by
/// <see cref="ShoppingListProjection"/> from the list event stream.
/// </summary>
public sealed class ShoppingListSummaryReadItem
{
	public Guid Id { get; set; }

	public string OwnerId { get; set; } = default!;

	public string Title { get; set; } = default!;

	public Guid? RecipeIdentifier { get; set; }

	public int ItemCount { get; set; }

	public DateTime CreatedAt { get; set; }

	public DateTime LastUpdated { get; set; }
}
