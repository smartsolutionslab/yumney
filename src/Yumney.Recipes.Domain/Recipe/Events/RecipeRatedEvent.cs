using SmartSolutionsLab.Yumney.Shared.Abstractions;

namespace SmartSolutionsLab.Yumney.Recipes.Domain.Recipe.Events;

public sealed record RecipeRatedEvent(
	RecipeIdentifier RecipeIdentifier,
	Rating Rating) : DomainEvent;
