using Yumney.Shared.Common;

namespace Yumney.Modules.Recipes.Domain.Recipe.Events;

public record RecipeDeletedEvent(RecipeId RecipeId) : DomainEvent;
