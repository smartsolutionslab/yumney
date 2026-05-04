using SmartSolutionsLab.Yumney.Shared.Abstractions;

namespace SmartSolutionsLab.Yumney.Recipes.Domain.Recipe.Events;

public sealed record RecipeSavedEvent(
	RecipeIdentifier RecipeIdentifier,
	RecipeTitle Title) : DomainEvent;
