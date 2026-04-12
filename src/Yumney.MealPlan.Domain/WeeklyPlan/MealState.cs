namespace SmartSolutionsLab.Yumney.MealPlan.Domain.WeeklyPlan;

/// <summary>
/// The preparation state of a meal slot.
/// </summary>
public enum MealState
{
    /// <summary>Meal is planned but not yet prepared.</summary>
    Planned,

    /// <summary>Meal was cooked/prepared.</summary>
    Cooked,

    /// <summary>Meal was skipped — ingredients stay at home.</summary>
    Skipped,
}
