namespace SmartSolutionsLab.Yumney.Shared.Events.Contracts;

/// <summary>
/// Published by the Recipes module when the user finishes cook mode for a
/// recipe (US-121). Each completion is a discrete event so subscribers can
/// count cooks; no dedup is applied here.
/// </summary>
public sealed record RecipeCookedIntegrationEvent(
	string OwnerId,
	Guid RecipeIdentifier,
	string RecipeTitle) : IntegrationEvent;
