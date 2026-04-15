namespace SmartSolutionsLab.Yumney.MealPlan.Application.DTOs;

public sealed record WeeklyPlanDto(string Week, bool IsExtendedMode, IReadOnlyList<MealSlotDto> Slots);
