namespace SmartSolutionsLab.Yumney.Shopping.Api.Requests;

public sealed record CreateShoppingListRequest(
    string Title,
    List<CreateShoppingListItemRequest> Items,
    Guid? RecipeReference = null);
