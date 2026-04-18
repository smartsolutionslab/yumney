namespace SmartSolutionsLab.Yumney.Shopping.Application.DTOs;

public sealed record ShoppingListSummaryDto(
	Guid Identifier,
	string Title,
	int ItemCount,
	DateTime CreatedAt);
