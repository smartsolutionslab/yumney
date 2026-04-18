using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shared.Guards;

namespace SmartSolutionsLab.Yumney.MealPlan.Domain.WeeklyPlan;

/// <summary>
/// A weekly meal plan — one per user per week.
/// Default mode: 7 Dinner slots. Extended mode: 21 slots (Breakfast + Lunch + Dinner).
/// </summary>
#pragma warning disable SA1311
public sealed class WeeklyPlan : AggregateRoot<WeeklyPlanIdentifier>
{
    private static readonly DayOfWeek[] allDays =
        [DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday, DayOfWeek.Saturday, DayOfWeek.Sunday];

    private readonly List<MealSlot> slots = [];

    public OwnerIdentifier Owner { get; private set; } = default!;

    public WeekIdentifier Week { get; private set; } = default!;

    public bool IsExtendedMode { get; private set; }

    public IReadOnlyList<MealSlot> Slots => slots.AsReadOnly();

    private WeeklyPlan()
    {
    }

    /// <summary>
    /// Create a new weekly plan with Dinner slots for each day (default mode).
    /// </summary>
    /// <param name="owner">The user who owns this plan.</param>
    /// <param name="week">The week this plan covers.</param>
    /// <param name="defaultServings">Default servings per slot (from household profile).</param>
    /// <returns>A new weekly plan with 7 empty Dinner slots.</returns>
    public static WeeklyPlan Create(OwnerIdentifier owner, WeekIdentifier week, int defaultServings = 4)
    {
        var plan = new WeeklyPlan
        {
            Id = WeeklyPlanIdentifier.New(),
            Owner = owner,
            Week = week,
        };

        foreach (var day in allDays)
            plan.slots.Add(MealSlot.Create(day, MealType.Dinner, defaultServings));

        return plan;
    }

    /// <summary>
    /// Enable extended mode — adds Breakfast and Lunch slots for each day.
    /// Existing Dinner slots are preserved.
    /// </summary>
    /// <param name="defaultServings">Default servings for new slots.</param>
    /// <returns></returns>
    public WeeklyPlan EnableExtendedMode(int defaultServings = 4)
    {
        if (IsExtendedMode) return this;

        foreach (var day in allDays)
        {
            if (!slots.Any(s => s.Day == day && s.MealType == MealType.Breakfast))
                slots.Add(MealSlot.Create(day, MealType.Breakfast, defaultServings));
            if (!slots.Any(s => s.Day == day && s.MealType == MealType.Lunch))
                slots.Add(MealSlot.Create(day, MealType.Lunch, defaultServings));
        }

        IsExtendedMode = true;
        return this;
    }

    /// <summary>
    /// Disable extended mode — hides Breakfast and Lunch slots but preserves their data.
    /// Only Dinner slots are visible in default mode.
    /// </summary>
    /// <returns></returns>
    public WeeklyPlan DisableExtendedMode()
    {
        IsExtendedMode = false;
        return this;
    }

    /// <summary>
    /// Get slots visible in the current mode.
    /// </summary>
    /// <returns>Dinner slots only in default mode, all slots in extended mode.</returns>
    public IReadOnlyList<MealSlot> GetVisibleSlots()
    {
        return IsExtendedMode
            ? slots.AsReadOnly()
            : slots.Where(s => s.MealType == MealType.Dinner).ToList().AsReadOnly();
    }

    /// <summary>
    /// Assign a recipe to a specific slot.
    /// </summary>
    /// <param name="day">The day of the week.</param>
    /// <param name="recipe">The recipe reference (identifier + title).</param>
    /// <param name="mealType">The meal type (defaults to Dinner).</param>
    /// <param name="servings">Optional serving count override.</param>
    /// <returns></returns>
    public WeeklyPlan AssignRecipe(DayOfWeek day, SlotRecipeReference recipe, MealType mealType = MealType.Dinner, int? servings = null)
    {
        var slot = FindSlot(day, mealType);
        slot.AssignRecipe(recipe, servings);
        return this;
    }

    /// <summary>
    /// Set a slot as freetext (eating out, pizza order, etc.). No shopping integration.
    /// </summary>
    /// <param name="day">The day of the week.</param>
    /// <param name="label">The freetext label.</param>
    /// <param name="mealType">The meal type (defaults to Dinner).</param>
    /// <returns></returns>
    public WeeklyPlan SetFreetext(DayOfWeek day, string label, MealType mealType = MealType.Dinner)
    {
        Ensure.That(label).IsNotNullOrWhiteSpace();
        var slot = FindSlot(day, mealType);
        slot.SetAsFreetext(label);
        return this;
    }

    /// <summary>
    /// Set a slot as leftovers from another meal. No new shopping items.
    /// </summary>
    /// <param name="day">The day of the week.</param>
    /// <param name="sourceDay">The day of the source meal.</param>
    /// <param name="sourceMealType">The meal type of the source meal.</param>
    /// <param name="sourceRecipeTitle">The source recipe title for display.</param>
    /// <param name="mealType">The meal type of this slot (defaults to Dinner).</param>
    /// <param name="servings">Optional serving count.</param>
    /// <returns></returns>
    public WeeklyPlan SetLeftover(DayOfWeek day, DayOfWeek sourceDay, MealType sourceMealType, string sourceRecipeTitle, MealType mealType = MealType.Dinner, int? servings = null)
    {
        Ensure.That(sourceRecipeTitle).IsNotNullOrWhiteSpace();
        var slot = FindSlot(day, mealType);
        slot.SetAsLeftover(sourceDay, sourceMealType, sourceRecipeTitle, servings);
        return this;
    }

    /// <summary>
    /// Clear a slot back to empty.
    /// </summary>
    /// <param name="day">The day of the week.</param>
    /// <param name="mealType">The meal type (defaults to Dinner).</param>
    /// <returns></returns>
    public WeeklyPlan ClearSlot(DayOfWeek day, MealType mealType = MealType.Dinner)
    {
        var slot = FindSlot(day, mealType);
        slot.ClearSlot();
        return this;
    }

    /// <summary>
    /// Adjust servings for a specific slot.
    /// </summary>
    /// <param name="day">The day of the week.</param>
    /// <param name="servings">The new serving count.</param>
    /// <param name="mealType">The meal type (defaults to Dinner).</param>
    /// <returns></returns>
    public WeeklyPlan AdjustServings(DayOfWeek day, int servings, MealType mealType = MealType.Dinner)
    {
        Ensure.That(servings).IsPositive();
        var slot = FindSlot(day, mealType);
        slot.AdjustServingsTo(servings);
        return this;
    }

    /// <summary>
    /// Mark a meal as cooked — ingredients consumed from balance sheet.
    /// </summary>
    /// <param name="day">The day of the week.</param>
    /// <param name="mealType">The meal type (defaults to Dinner).</param>
    /// <returns></returns>
    public WeeklyPlan MarkAsCooked(DayOfWeek day, MealType mealType = MealType.Dinner)
    {
        var slot = FindSlot(day, mealType);
        slot.MarkAsCooked();
        return this;
    }

    /// <summary>
    /// Mark a meal as skipped — ingredients stay at home.
    /// </summary>
    /// <param name="day">The day of the week.</param>
    /// <param name="mealType">The meal type (defaults to Dinner).</param>
    /// <returns></returns>
    public WeeklyPlan MarkAsSkipped(DayOfWeek day, MealType mealType = MealType.Dinner)
    {
        var slot = FindSlot(day, mealType);
        slot.MarkAsSkipped();
        return this;
    }

    /// <summary>
    /// Reset a cooked/skipped meal back to planned.
    /// </summary>
    /// <param name="day">The day of the week.</param>
    /// <param name="mealType">The meal type (defaults to Dinner).</param>
    /// <returns></returns>
    public WeeklyPlan ResetToPlanned(DayOfWeek day, MealType mealType = MealType.Dinner)
    {
        var slot = FindSlot(day, mealType);
        slot.ResetToPlanned();
        return this;
    }

    /// <summary>
    /// Get meals that need cooked confirmation (planned recipes from past days).
    /// </summary>
    /// <param name="today">Today's day of week.</param>
    /// <returns>Slots that are Recipe type, still in Planned state, and before today.</returns>
    public IReadOnlyList<MealSlot> GetUnconfirmedPastMeals(DayOfWeek today)
    {
        return slots
            .Where(s => s.ContentType == SlotContentType.Recipe && s.State == MealState.Planned && s.Day < today)
            .ToList();
    }

    /// <summary>
    /// Swap the meals between two slots.
    /// </summary>
    /// <param name="day1">First day.</param>
    /// <param name="day2">Second day.</param>
    /// <param name="mealType">The meal type to swap (defaults to Dinner).</param>
    /// <returns></returns>
    public WeeklyPlan SwapSlots(DayOfWeek day1, DayOfWeek day2, MealType mealType = MealType.Dinner)
    {
        var slot1 = FindSlot(day1, mealType);
        var slot2 = FindSlot(day2, mealType);

        var snapshot1 = slot1.TakeSnapshot();
        slot1.RestoreFromSnapshot(slot2.TakeSnapshot());
        slot2.RestoreFromSnapshot(snapshot1);
        return this;
    }

    private MealSlot FindSlot(DayOfWeek day, MealType mealType)
    {
        return slots.FirstOrDefault(s => s.Day == day && s.MealType == mealType)
            ?? throw new EntityNotFoundException(nameof(MealSlot), $"{day}/{mealType}");
    }
}
#pragma warning restore SA1311
