using SmartSolutionsLab.Yumney.Shared.Abstractions;

namespace SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingList.Events;

public sealed record ShoppingListCreated(
	ShoppingListIdentifier Identifier,
	ShoppingListTitle Title,
	OwnerIdentifier Owner,
	RecipeReference? RecipeReference,
	DateTime CreatedAt) : DomainEvent;
