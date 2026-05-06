using SmartSolutionsLab.Yumney.MealPlan.Domain.WeeklyPlan;
using SmartSolutionsLab.Yumney.MealPlan.Domain.WeeklyPlan.Events;

namespace SmartSolutionsLab.Yumney.MealPlan.Application.DTOs;

public static class WeeklyPlanMappingExtensions
{
	public static WeeklyPlanDto ToDto(this WeeklyPlan plan) =>
		new(plan.Week.Value, plan.IsExtendedMode, plan.GetVisibleSlots().ToDtos());

	public static MealSlotDto ToDto(this MealSlot slot) =>
		new(
			slot.Day.ToString(),
			slot.MealType.ToString(),
			slot.ContentType.ToString(),
			slot.State.ToString(),
			slot.Recipe?.RecipeIdentifier.Value,
			slot.Recipe?.Title.Value,
			slot.Servings.Value,
			slot.FreetextLabel?.Value,
			slot.LeftoverLabel?.Value,
			slot.LeftoverSourceDay?.ToString(),
			slot.LeftoverSourceMealType?.ToString(),
			slot.IsEmpty);

	public static IReadOnlyList<MealSlotDto> ToDtos(this IEnumerable<MealSlot> slots) =>
		slots
			.OrderBy(slot => slot.Day)
			.ThenBy(slot => slot.MealType)
			.Select(slot => slot.ToDto())
			.ToList();

	public static IReadOnlyList<CookedIngredient> ToCookedIngredients(this IEnumerable<RecipeIngredientLookupResult> ingredients) =>
		ingredients
			.Where(ingredient => ingredient.Amount.HasValue && ingredient.Amount.Value > 0m)
			.Select(ingredient => new CookedIngredient(ingredient.Name, ingredient.Amount!.Value, ingredient.Unit))
			.ToList();
}
