using SmartSolutionsLab.Yumney.Shared.Common;

namespace SmartSolutionsLab.Yumney.Recipes.Domain.Recipe.Events;

public sealed record RecipeSavedEvent(RecipeIdentifier RecipeIdentifier, RecipeTitle Title) : DomainEvent;
