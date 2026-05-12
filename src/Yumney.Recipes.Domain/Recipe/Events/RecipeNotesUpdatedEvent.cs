using SmartSolutionsLab.Yumney.Shared.Abstractions;

namespace SmartSolutionsLab.Yumney.Recipes.Domain.Recipe.Events;

public sealed record RecipeNotesUpdatedEvent(
	RecipeIdentifier RecipeIdentifier,
	bool HasNotes) : DomainEvent;
