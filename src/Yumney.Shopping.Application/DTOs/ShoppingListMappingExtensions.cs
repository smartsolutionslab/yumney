using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingList;

namespace SmartSolutionsLab.Yumney.Shopping.Application.DTOs;

public static class ShoppingListMappingExtensions
{
    extension(ShoppingList shoppingList)
    {
        public ShoppingListDetailDto ToDetailDto()
        {
            return new ShoppingListDetailDto(
                shoppingList.Id.Value,
                shoppingList.Title.Value,
                shoppingList.RecipeReference?.Value,
                shoppingList.CreatedAt,
                shoppingList.Items.Select(i => i.ToDto()).ToList());
        }

        public ShoppingListSummaryDto ToSummaryDto()
        {
            return new ShoppingListSummaryDto(
                shoppingList.Id.Value,
                shoppingList.Title.Value,
                shoppingList.Items.Count,
                shoppingList.CreatedAt);
        }
    }

    public static ShoppingListItemDto ToDto(this ShoppingListItem item)
    {
        return new ShoppingListItemDto(
            item.Id,
            item.Name.Value,
            item.Amount?.Value,
            item.Unit?.Value,
            item.IsChecked);
    }
}
