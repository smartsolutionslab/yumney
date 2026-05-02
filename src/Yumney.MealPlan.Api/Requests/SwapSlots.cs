using SmartSolutionsLab.Yumney.MealPlan.Domain.WeeklyPlan;

namespace SmartSolutionsLab.Yumney.MealPlan.Api.Requests;

public sealed record SwapSlots(
	DayOfWeek SourceDay,
	DayOfWeek TargetDay,
	MealType MealType = MealType.Dinner);
