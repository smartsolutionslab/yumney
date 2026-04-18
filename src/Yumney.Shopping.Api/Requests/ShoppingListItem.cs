namespace SmartSolutionsLab.Yumney.Shopping.Api.Requests;

public sealed record ShoppingListItem(
	string Name,
	decimal? Amount,
	string? Unit);
