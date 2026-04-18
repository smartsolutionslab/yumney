namespace SmartSolutionsLab.Yumney.MealPlan.Domain.WeeklyPlan;

/// <summary>
/// Tracks the lifecycle state of a planned meal.
/// </summary>
public enum MealState
{
	/// <summary>The meal is planned but not yet prepared.</summary>
	Planned,

	/// <summary>The meal has been prepared and consumed.</summary>
	Cooked,

	/// <summary>The meal was skipped.</summary>
	Skipped,
}
