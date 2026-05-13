using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingList;
using CommandItem = SmartSolutionsLab.Yumney.Shopping.Application.Commands.ShoppingListItem;

namespace SmartSolutionsLab.Yumney.Shopping.Api.Requests;

public sealed record CreateShoppingList(
	string Title,
	List<ShoppingListItem> Items,
	Guid? RecipeIdentifier = null)
{
	public (ShoppingListTitle Title, IReadOnlyList<CommandItem> Items, RecipeReference? RecipeReference) ToValueObjects() =>
	(
		ShoppingListTitle.From(Title),
		Items.Select(item => new CommandItem(
			ItemName.From(item.Name),
			Quantity.FromNullable(
				Amount.FromNullable(item.Amount),
				Unit.FromNullable(item.Unit))))
			.ToList(),
		RecipeReference.FromNullable(RecipeIdentifier));
}
