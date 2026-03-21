namespace SmartSolutionsLab.Yumney.Shopping.Application.Requests;

public sealed record CreateShoppingListRequest(
    string Title,
    List<CreateShoppingListItemRequest> Items,
    Guid? RecipeIdentifier = null);
