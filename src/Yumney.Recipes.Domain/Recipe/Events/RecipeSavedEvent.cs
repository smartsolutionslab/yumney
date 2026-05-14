using SmartSolutionsLab.Yumney.Shared.Abstractions;

namespace SmartSolutionsLab.Yumney.Recipes.Domain.Recipe.Events;

public sealed record RecipeSavedEvent(
	RecipeIdentifier Recipe,
	RecipeTitle Title) : DomainEvent;
