namespace SmartSolutionsLab.Yumney.Shared.Events.CrossModule;

/// <summary>
/// Published by the Recipes module after a recipe has been deleted.
/// Subscribers (e.g. Shopping) use this to clean up any cross-module
/// references to the deleted recipe.
/// </summary>
public sealed record RecipeDeletedIntegrationEvent(
	string OwnerId,
	Guid RecipeIdentifier) : IntegrationEvent;
