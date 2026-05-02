using SmartSolutionsLab.Yumney.MealPlan.Domain.WeeklyPlan;

namespace SmartSolutionsLab.Yumney.MealPlan.Api.Requests;

public sealed record AdjustServings(DayOfWeek Day, MealType MealType, int Servings);
