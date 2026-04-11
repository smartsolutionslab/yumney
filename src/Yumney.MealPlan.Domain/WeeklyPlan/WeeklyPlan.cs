using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shared.Guards;

namespace SmartSolutionsLab.Yumney.MealPlan.Domain.WeeklyPlan;

/// <summary>
/// A weekly meal plan — one per user per week.
/// Contains 7 meal slots (one per day, default mode).
/// </summary>
public sealed class WeeklyPlan : AggregateRoot<WeeklyPlanIdentifier>
{
    private readonly List<MealSlot> slots = [];

    public OwnerIdentifier Owner { get; private set; } = default!;

    public WeekIdentifier Week { get; private set; } = default!;

    public IReadOnlyList<MealSlot> Slots => slots.AsReadOnly();

    private WeeklyPlan()
    {
    }

    /// <summary>
    /// Create a new weekly plan with empty slots for each day.
    /// </summary>
    /// <param name="owner">The user who owns this plan.</param>
    /// <param name="week">The week this plan covers.</param>
    /// <param name="defaultServings">Default servings per slot (from household profile).</param>
    /// <returns>A new weekly plan with 7 empty slots.</returns>
    public static WeeklyPlan Create(OwnerIdentifier owner, WeekIdentifier week, int defaultServings = 4)
    {
        var plan = new WeeklyPlan
        {
            Id = WeeklyPlanIdentifier.New(),
            Owner = owner,
            Week = week,
        };

        plan.slots.Add(MealSlot.Create(DayOfWeek.Monday, defaultServings));
        plan.slots.Add(MealSlot.Create(DayOfWeek.Tuesday, defaultServings));
        plan.slots.Add(MealSlot.Create(DayOfWeek.Wednesday, defaultServings));
        plan.slots.Add(MealSlot.Create(DayOfWeek.Thursday, defaultServings));
        plan.slots.Add(MealSlot.Create(DayOfWeek.Friday, defaultServings));
        plan.slots.Add(MealSlot.Create(DayOfWeek.Saturday, defaultServings));
        plan.slots.Add(MealSlot.Create(DayOfWeek.Sunday, defaultServings));

        return plan;
    }

    /// <summary>
    /// Assign a recipe to a specific day's slot.
    /// </summary>
    /// <param name="day">The day of the week.</param>
    /// <param name="recipeIdentifier">The recipe identifier.</param>
    /// <param name="recipeTitle">The recipe title for display.</param>
    /// <param name="servings">Optional serving count override.</param>
    public void AssignRecipe(DayOfWeek day, Guid recipeIdentifier, string recipeTitle, int? servings = null)
    {
        Ensure.That(recipeTitle).IsNotNullOrWhiteSpace();
        var slot = FindSlot(day);
        slot.AssignRecipe(recipeIdentifier, recipeTitle, servings);
    }

    /// <summary>
    /// Remove the recipe from a specific day's slot.
    /// </summary>
    /// <param name="day">The day of the week.</param>
    public void ClearSlot(DayOfWeek day)
    {
        var slot = FindSlot(day);
        slot.ClearRecipe();
    }

    /// <summary>
    /// Swap the meals between two days.
    /// </summary>
    /// <param name="day1">First day.</param>
    /// <param name="day2">Second day.</param>
    public void SwapSlots(DayOfWeek day1, DayOfWeek day2)
    {
        var slot1 = FindSlot(day1);
        var slot2 = FindSlot(day2);

        var tempRecipe = slot1.RecipeIdentifier;
        var tempTitle = slot1.RecipeTitle;
        var tempServings = slot1.Servings;

        if (slot2.RecipeIdentifier.HasValue)
            slot1.AssignRecipe(slot2.RecipeIdentifier.Value, slot2.RecipeTitle!, slot2.Servings);
        else
            slot1.ClearRecipe();

        if (tempRecipe.HasValue)
            slot2.AssignRecipe(tempRecipe.Value, tempTitle!, tempServings);
        else
            slot2.ClearRecipe();
    }

    private MealSlot FindSlot(DayOfWeek day)
    {
        return slots.FirstOrDefault(s => s.Day == day)
            ?? throw new EntityNotFoundException(nameof(MealSlot), day.ToString());
    }
}
