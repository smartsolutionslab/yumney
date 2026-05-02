using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingList;

namespace SmartSolutionsLab.Yumney.Shopping.Api.Requests;

public sealed record MarkAsFrozen(string Name, string? Unit = null)
{
	public (ItemName ItemName, Unit? Unit) ToValueObjects() =>
		(ItemName.From(Name), Shopping.Domain.ShoppingList.Unit.FromNullable(Unit));
}
