namespace SmartSolutionsLab.Yumney.Shopping.Api.Requests;

public sealed record CreateShoppingListRequest(
    string Title,
    List<ShoppingListItem> Items,
    Guid? RecipeReference = null);
