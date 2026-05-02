using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingList;

namespace SmartSolutionsLab.Yumney.Shopping.Api.Requests;

public sealed record RemoveItem(string Name, decimal? Quantity = null, string? Unit = null, string? Reason = null)
{
	public void Deconstruct(out ItemName itemName, out Quantity? quantity, out RemovalReason? reason)
	{
		itemName = ItemName.From(Name);
		quantity = Shopping.Domain.ShoppingList.Quantity.FromNullable(
			Amount.FromNullable(Quantity),
			Shopping.Domain.ShoppingList.Unit.FromNullable(Unit));
		reason = RemovalReason.FromNullable(Reason);
	}
}
