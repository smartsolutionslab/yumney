namespace SmartSolutionsLab.Yumney.MealPlan.Domain.WeeklyPlan;

/// <summary>
/// The type of meal for a slot.
/// </summary>
public enum MealType
{
    /// <summary>Dinner (default, always visible).</summary>
    Dinner,

    /// <summary>Breakfast (extended mode only).</summary>
    Breakfast,

    /// <summary>Lunch (extended mode only).</summary>
    Lunch,
}
