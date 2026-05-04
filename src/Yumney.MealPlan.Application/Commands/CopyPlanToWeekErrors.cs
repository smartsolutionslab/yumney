using SmartSolutionsLab.Yumney.Shared.Outcomes;

namespace SmartSolutionsLab.Yumney.MealPlan.Application.Commands;

public static class CopyPlanToWeekErrors
{
	public static readonly ApiError SourcePlanNotFound = new("MEAL_PLAN_SOURCE_NOT_FOUND", "No meal plan found for the source week.", 404);
	public static readonly ApiError SameWeek = new("MEAL_PLAN_COPY_SAME_WEEK", "Source and target week must be different.", 422);
}
