using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingLedger;
using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingList;
using ShoppingList = SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingList;

namespace SmartSolutionsLab.Yumney.Shopping.Api.Requests;

public sealed record AddManualItem(
	string Name,
	decimal? Quantity = null,
	string? Unit = null,
	string? Source = null)
{
	public void Deconstruct(out ItemName itemName, out Quantity? quantity, out ItemSource source)
	{
		itemName = ItemName.From(Name);
		quantity = ShoppingList.Quantity.FromNullable(
			Amount.FromNullable(Quantity),
			ShoppingList.Unit.FromNullable(Unit));
		source = string.IsNullOrWhiteSpace(Source) ? ItemSource.Manual : ItemSource.From(Source);
	}
}
