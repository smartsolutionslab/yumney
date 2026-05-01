using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingLedger;
using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingList;

namespace SmartSolutionsLab.Yumney.Shopping.Api.Requests;

public sealed record AddManualItemRequest(string Name, decimal? Quantity = null, string? Unit = null, string? Source = null)
{
	public void Deconstruct(out ItemName itemName, out Quantity? quantity, out ItemSource source)
	{
		itemName = ItemName.From(Name.Trim());
		quantity = Shopping.Domain.ShoppingList.Quantity.FromNullable(
			Amount.FromNullable(Quantity),
			Shopping.Domain.ShoppingList.Unit.FromNullable(Unit));
		source = string.IsNullOrWhiteSpace(Source) ? ItemSource.Manual : ItemSource.From(Source);
	}
}
