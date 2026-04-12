namespace SmartSolutionsLab.Yumney.MealPlan.Domain.WeeklyPlan;

/// <summary>
/// The type of content in a meal slot.
/// </summary>
public enum SlotContentType
{
    /// <summary>No plan for this slot.</summary>
    Empty,

    /// <summary>Linked to a saved recipe with full ingredient/shopping integration.</summary>
    Recipe,

    /// <summary>Leftovers from another meal — no new shopping items.</summary>
    Leftover,

    /// <summary>Freetext label only — eating out, pizza order, etc.</summary>
    Freetext,
}
