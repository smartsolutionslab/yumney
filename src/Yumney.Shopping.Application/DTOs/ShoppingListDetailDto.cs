namespace SmartSolutionsLab.Yumney.Shopping.Application.DTOs;

public sealed record ShoppingListDetailDto(
	Guid Identifier,
	string Title,
	Guid? RecipeReference,
	DateTime CreatedAt,
	IReadOnlyList<ShoppingListItemDto> Items);
