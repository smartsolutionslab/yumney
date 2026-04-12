using SmartSolutionsLab.Yumney.MealPlan.Domain.WeeklyPlan;

namespace SmartSolutionsLab.Yumney.MealPlan.Api.Requests;

public sealed record ConfirmMealRequest(DayOfWeek Day, MealType MealType, MealState State);
