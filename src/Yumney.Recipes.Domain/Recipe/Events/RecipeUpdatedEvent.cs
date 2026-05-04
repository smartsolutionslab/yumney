using SmartSolutionsLab.Yumney.Shared.Abstractions;

namespace SmartSolutionsLab.Yumney.Recipes.Domain.Recipe.Events;

public sealed record RecipeUpdatedEvent(
	RecipeIdentifier RecipeIdentifier,
	RecipeTitle Title) : DomainEvent;
