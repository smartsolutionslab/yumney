using SmartSolutionsLab.Yumney.Shared.Common;

namespace SmartSolutionsLab.Yumney.Recipes.Domain.Recipe.Events;

public sealed record RecipeUpdatedEvent(RecipeIdentifier RecipeIdentifier, RecipeTitle Title) : DomainEvent;
