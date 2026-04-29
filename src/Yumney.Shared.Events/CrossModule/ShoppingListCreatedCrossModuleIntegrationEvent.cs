namespace SmartSolutionsLab.Yumney.Shared.Events.CrossModule;

/// <summary>
/// Published by the Shopping module when a new <c>ShoppingList</c> aggregate
/// is created. Carries primitive types only so cross-module subscribers
/// (Recipes, MealPlan) can react without taking a Shopping.Domain dependency.
/// </summary>
public sealed record ShoppingListCreatedCrossModuleIntegrationEvent(
	string OwnerId,
	Guid ListIdentifier,
	string Title,
	Guid? RecipeIdentifier,
	DateTime CreatedAt) : IntegrationEvent;
