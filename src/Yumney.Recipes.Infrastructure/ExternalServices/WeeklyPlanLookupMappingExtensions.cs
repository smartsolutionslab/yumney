using SmartSolutionsLab.Yumney.MealPlan.Client;
using SmartSolutionsLab.Yumney.Recipes.Application.Interfaces;

namespace SmartSolutionsLab.Yumney.Recipes.Infrastructure.ExternalServices;

internal static class WeeklyPlanLookupMappingExtensions
{
	public static WeeklyPlanLookupResult ToLookupResult(this WeeklyPlanResponse response) =>
		new(
			response.Week,
			response.IsExtendedMode,
			[.. response.Slots.Select(slot => slot.ToLookupSlot())]);

	public static WeeklyPlanLookupSlot ToLookupSlot(this WeeklyPlanSlotResponse slot) =>
		new(slot.Day, slot.MealType, slot.RecipeIdentifier, slot.RecipeTitle, slot.Servings, slot.IsEmpty);
}
