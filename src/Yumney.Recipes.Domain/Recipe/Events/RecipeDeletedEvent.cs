using SmartSolutionsLab.Yumney.Shared.Abstractions;

namespace SmartSolutionsLab.Yumney.Recipes.Domain.Recipe.Events;

public sealed record RecipeDeletedEvent(
	RecipeIdentifier Recipe,
	RecipeTitle Title,
	OwnerIdentifier Owner) : DomainEvent;
