namespace SmartSolutionsLab.Yumney.Shopping.Infrastructure.Persistence.EventStore;

/// <summary>
/// Tracks identity and ownership for one <see cref="SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingList.ShoppingList"/>
/// aggregate. Multiple rows per owner (one per list).
/// </summary>
public sealed class ShoppingListAggregateMetadata
{
	public Guid AggregateId { get; set; }

	public string OwnerId { get; set; } = default!;
}
