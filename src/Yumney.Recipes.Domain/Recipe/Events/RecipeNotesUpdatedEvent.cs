using SmartSolutionsLab.Yumney.Shared.Common;

namespace SmartSolutionsLab.Yumney.Recipes.Domain.Recipe.Events;

public sealed record RecipeNotesUpdatedEvent(RecipeIdentifier RecipeIdentifier, bool HasNotes) : DomainEvent;
