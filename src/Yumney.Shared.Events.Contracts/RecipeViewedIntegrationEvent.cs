namespace SmartSolutionsLab.Yumney.Shared.Events.Contracts;

/// <summary>
/// Published by the Recipes module when a user opens a recipe's detail view
/// (US-121). The publisher applies a per-(user, recipe) dedup window so
/// subscribers don't need to. Carries no aggregate state — it's a pure
/// notification used by the activity log.
/// </summary>
public sealed record RecipeViewedIntegrationEvent(
	string OwnerId,
	Guid RecipeIdentifier,
	string RecipeTitle) : IntegrationEvent;
