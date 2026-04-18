using SmartSolutionsLab.Yumney.Shared.Common;

namespace SmartSolutionsLab.Yumney.Shopping.Application.Commands;

public static class CheckOffItemErrors
{
	public static readonly ApiError ListNotFound = new("SHOPPING_LIST_NOT_FOUND", "Shopping list not found.", 404);

	public static readonly ApiError AccessDenied = new("SHOPPING_LIST_ACCESS_DENIED", "Shopping list not found.", 404);
}
