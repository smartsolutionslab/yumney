namespace SmartSolutionsLab.Yumney.Shared.Events.CrossModule;

/// <summary>
/// Published by the Shopping module when a list's recipe-reference link is
/// cleared (for instance, in response to <see cref="RecipeDeletedIntegrationEvent"/>).
/// Lets cross-module subscribers reconcile any state that depended on the
/// recipe→list pairing.
/// </summary>
public sealed record ShoppingListRecipeReferenceClearedCrossModuleIntegrationEvent(
	string OwnerId,
	Guid ListIdentifier) : IntegrationEvent;
