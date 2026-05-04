using SmartSolutionsLab.Yumney.Shared.Paging;

namespace SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingList;

public sealed record ShoppingListSummary(
	ShoppingListIdentifier Identifier,
	ShoppingListTitle Title,
	ItemCount ItemCount,
	DateTime CreatedAt);
