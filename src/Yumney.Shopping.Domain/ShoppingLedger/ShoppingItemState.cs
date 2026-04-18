using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingList;

namespace SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingLedger;

#pragma warning disable SA1600
public sealed record ShoppingItemState
{
	public ItemName ItemName { get; init; } = default!;

	public Unit? Unit { get; init; }

	public Amount OnList { get; init; } = Amount.From(0);

	public Amount Bought { get; init; } = Amount.From(0);

	public Amount Consumed { get; init; } = Amount.From(0);

	public Amount Removed { get; init; } = Amount.From(0);

	public Amount AtHome => Amount.From(Math.Max(0, Bought.Value - Consumed.Value - Removed.Value));

	public decimal Remaining => OnList.Value - Bought.Value;

	public bool IsBought => Bought.Value > 0;

	public string GroupKey => $"{ItemName.Value.ToLowerInvariant()}|{Unit?.Value ?? string.Empty}";
}
