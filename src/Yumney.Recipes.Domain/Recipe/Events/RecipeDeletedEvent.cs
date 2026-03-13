using SmartSolutionsLab.Yumney.Shared.Common;

namespace SmartSolutionsLab.Yumney.Recipes.Domain.Recipe.Events;

public sealed record RecipeDeletedEvent(
    RecipeIdentifier RecipeIdentifier, RecipeTitle Title, OwnerIdentifier Owner) : DomainEvent;
