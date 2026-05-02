using SmartSolutionsLab.Yumney.MealPlan.Domain.WeeklyPlan;

namespace SmartSolutionsLab.Yumney.MealPlan.Api.Requests;

public sealed record ClearSlot(DayOfWeek Day, MealType MealType = MealType.Dinner);
