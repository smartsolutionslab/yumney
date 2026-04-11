using SmartSolutionsLab.Yumney.Shared.Common;

namespace SmartSolutionsLab.Yumney.MealPlan.Domain.WeeklyPlan;

/// <summary>
/// A single meal slot within a weekly plan — one per day (default mode).
/// </summary>
public sealed class MealSlot : Entity<MealSlotIdentifier>
{
    public DayOfWeek Day { get; private set; }

    public Guid? RecipeIdentifier { get; private set; }

    public string? RecipeTitle { get; private set; }

    public int Servings { get; private set; }

    public bool IsEmpty => RecipeIdentifier is null;

    private MealSlot()
    {
    }

    internal static MealSlot Create(DayOfWeek day, int defaultServings)
    {
        return new MealSlot
        {
            Id = MealSlotIdentifier.New(),
            Day = day,
            Servings = defaultServings,
        };
    }

    internal void AssignRecipe(Guid recipeIdentifier, string recipeTitle, int? servings = null)
    {
        RecipeIdentifier = recipeIdentifier;
        RecipeTitle = recipeTitle;
        if (servings.HasValue)
            Servings = servings.Value;
    }

    internal void ClearRecipe()
    {
        RecipeIdentifier = null;
        RecipeTitle = null;
    }

    internal void AdjustServingsTo(int servings)
    {
        Servings = servings;
    }
}
