using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingList;
using CommandItem = SmartSolutionsLab.Yumney.Shopping.Application.Commands.ShoppingListItem;

namespace SmartSolutionsLab.Yumney.Shopping.Api.Requests;

public sealed record CreateShoppingListRequest(
    string Title,
    List<ShoppingListItem> Items,
    Guid? RecipeReference = null)
{
    public (ShoppingListTitle Title, IReadOnlyList<CommandItem> Items, RecipeReference? RecipeReference) ToValueObjects() =>
    (
        ShoppingListTitle.From(Title),
        Items.Select(i => new CommandItem(
            ItemName.From(i.Name),
            Quantity.FromNullable(
                Amount.FromNullable(i.Amount),
                Unit.FromNullable(i.Unit))))
            .ToList(),
        Domain.ShoppingList.RecipeReference.FromNullable(RecipeReference));
}
