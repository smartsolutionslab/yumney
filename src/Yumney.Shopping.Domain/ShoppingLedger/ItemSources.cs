namespace SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingLedger;

/// <summary>
/// Constants for item source tracking.
/// </summary>
public static class ItemSources
{
    /// <summary>Gets the source value for manually added items.</summary>
    public const string Manual = "manual";

    /// <summary>Gets the source value for items generated from a meal plan.</summary>
    public const string MealPlan = "meal-plan";
}
