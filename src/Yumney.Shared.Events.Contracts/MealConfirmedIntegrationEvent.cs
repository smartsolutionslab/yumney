namespace SmartSolutionsLab.Yumney.Shared.Events.Contracts;

/// <summary>
/// Published by the MealPlan module when a planned meal transitions to the
/// <c>Cooked</c> state. Carries the ingredient list at the time of confirmation
/// so downstream subscribers (Shopping) can apply per-ingredient side effects
/// without a sync call back into Recipes.
/// </summary>
public sealed record MealConfirmedIntegrationEvent(
	string OwnerId,
	Guid RecipeIdentifier,
	int Servings,
	IReadOnlyList<MealConfirmedIngredient> Ingredients) : IntegrationEvent;
