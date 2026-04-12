using SmartSolutionsLab.Yumney.MealPlan.Domain.WeeklyPlan;

namespace SmartSolutionsLab.Yumney.MealPlan.Api.Requests;

public sealed record ClearSlotRequest(DayOfWeek Day, MealType MealType = MealType.Dinner);
