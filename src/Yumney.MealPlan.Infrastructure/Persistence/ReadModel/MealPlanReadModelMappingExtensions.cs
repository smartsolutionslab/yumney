using SmartSolutionsLab.Yumney.MealPlan.Application.DTOs;
using SmartSolutionsLab.Yumney.MealPlan.Domain.WeeklyPlan;

namespace SmartSolutionsLab.Yumney.MealPlan.Infrastructure.Persistence.ReadModel;

public static class MealPlanReadModelMappingExtensions
{
	public static MealSlotDto ToDto(this MealPlanSlotReadItem row) =>
		new(
			row.Day,
			row.MealType,
			row.ContentType,
			row.State,
			row.RecipeIdentifier,
			row.RecipeTitle,
			row.Servings,
			row.FreetextLabel,
			row.LeftoverLabel,
			row.LeftoverSourceDay,
			row.LeftoverSourceMealType,
			row.ContentType == nameof(SlotContentType.Empty));

	public static IReadOnlyList<MealSlotDto> ToDtos(this IEnumerable<MealPlanSlotReadItem> rows) =>
		rows
			.Select(row => row.ToDto())
			.OrderBy(slot => Enum.Parse<DayOfWeek>(slot.Day))
			.ThenBy(slot => Enum.Parse<MealType>(slot.MealType))
			.ToList();

	public static PlannedRecipeDto ToPlannedRecipeDto(this MealPlanSlotReadItem row) =>
		new(
			row.RecipeIdentifier!.Value,
			row.RecipeTitle ?? string.Empty,
			row.Servings,
			row.Day,
			row.MealType);

	public static IReadOnlyList<PlannedRecipeDto> ToPlannedRecipeDtos(this IEnumerable<MealPlanSlotReadItem> rows) =>
		rows
			.Select(row => row.ToPlannedRecipeDto())
			.ToList();

	public static MealHistoryEntryDto ToHistoryEntryDto(this MealPlanSlotReadItem row) =>
		new(
			row.RecipeIdentifier,
			row.RecipeTitle ?? string.Empty,
			row.Week,
			row.Day,
			row.MealType);

	public static IReadOnlyList<MealHistoryEntryDto> ToHistoryEntryDtos(this IEnumerable<MealPlanSlotReadItem> rows) =>
		rows
			.Select(row => row.ToHistoryEntryDto())
			.ToList();
}
