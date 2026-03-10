using Yumney.Shared.Common;

namespace Yumney.Recipes.Domain.Recipe.Events;

public sealed record RecipeSavedEvent(Guid RecipeIdentifier, RecipeTitle Title) : DomainEvent;
