using SmartSolutionsLab.Yumney.MealPlan.Domain.WeeklyPlan;

namespace SmartSolutionsLab.Yumney.MealPlan.Application.DTOs;

public static class MealSlotMappingExtensions
{
	public static MealSlotDto ToDto(this MealSlot slot)
	{
		return new MealSlotDto(
			slot.Day.ToString(),
			slot.MealType.ToString(),
			slot.ContentType.ToString(),
			slot.State.ToString(),
			slot.Recipe?.RecipeIdentifier,
			slot.Recipe?.Title,
			slot.Servings.Value,
			slot.FreetextLabel?.Value,
			slot.LeftoverLabel?.Value,
			slot.LeftoverSourceDay?.ToString(),
			slot.LeftoverSourceMealType?.ToString(),
			slot.IsEmpty);
	}

	public static List<MealSlotDto> ToOrderedDtos(this IEnumerable<MealSlot> slots)
	{
		return slots
			.OrderBy(s => s.Day)
			.ThenBy(s => s.MealType)
			.Select(s => s.ToDto())
			.ToList();
	}
}
