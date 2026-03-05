using Yumney.Shared.Common;

namespace Yumney.Modules.Recipes.Domain.Recipe.Events;

public record RecipeImportedEvent(
    RecipeId RecipeId,
    RecipeTitle Title,
    SourceUrl Source) : DomainEvent;
