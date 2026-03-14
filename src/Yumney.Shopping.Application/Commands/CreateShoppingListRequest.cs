namespace SmartSolutionsLab.Yumney.Shopping.Application.Commands;

public sealed record CreateShoppingListRequest(
    string Title,
    List<CreateShoppingListItemRequest> Items,
    Guid? RecipeIdentifier = null);
