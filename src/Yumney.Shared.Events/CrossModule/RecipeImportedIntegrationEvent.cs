namespace SmartSolutionsLab.Yumney.Shared.Events.CrossModule;

/// <summary>
/// Published by the Recipes module after a recipe is saved (manually or via
/// import). Subscribers (e.g. Users) record activity for the smart dashboard
/// and recipe history (US-121).
/// </summary>
public sealed record RecipeImportedIntegrationEvent(
	string OwnerId,
	Guid RecipeIdentifier,
	string RecipeTitle) : IntegrationEvent;
