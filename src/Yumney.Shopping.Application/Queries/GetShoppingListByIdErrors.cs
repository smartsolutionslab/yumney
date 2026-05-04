using SmartSolutionsLab.Yumney.Shared.Outcomes;

namespace SmartSolutionsLab.Yumney.Shopping.Application.Queries;

public static class GetShoppingListByIdErrors
{
	public static readonly ApiError NotFound = new("SHOPPING_LIST_NOT_FOUND", "Shopping list not found.", 404);

	public static readonly ApiError AccessDenied = new("SHOPPING_LIST_ACCESS_DENIED", "Shopping list not found.", 404);
}
