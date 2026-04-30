using SmartSolutionsLab.Yumney.Shared.Common;

namespace SmartSolutionsLab.Yumney.Shopping.Infrastructure.Persistence.ReadModel;

/// <summary>
/// Per-ingredient at-home balance derived from ledger events.
/// One row per (OwnerId, ItemName, Unit). Maintained asynchronously
/// by <see cref="IngredientBalanceProjectionHandler"/>.
/// </summary>
public sealed class IngredientBalanceReadItem
{
	public Guid Id { get; set; }

	public string OwnerId { get; set; } = default!;

	public string ItemName { get; set; } = default!;

	/// <summary>
	/// Gets or sets the lowercased <see cref="ItemName"/> used for case-insensitive lookup.
	/// Maintained by the projection so SQL stays simple and provider-portable.
	/// </summary>
	public string NameKey { get; set; } = default!;

	public string? Unit { get; set; }

	public string Category { get; set; } = IngredientCategory.Other.Value;

	public decimal BoughtTotal { get; set; }

	public decimal ConsumedTotal { get; set; }

	public decimal RemovedTotal { get; set; }

	/// <summary>
	/// Gets or sets the most recent Bought / AddedAsAtHome timestamp,
	/// used to compute freshness (US-341). Null until the first purchase.
	/// </summary>
	public DateTime? LastBoughtAt { get; set; }

	public DateTime LastUpdated { get; set; }

	/// <summary>
	/// Gets the current at-home balance: never negative.
	/// </summary>
	public decimal AtHome => Math.Max(0, BoughtTotal - ConsumedTotal - RemovedTotal);
}
