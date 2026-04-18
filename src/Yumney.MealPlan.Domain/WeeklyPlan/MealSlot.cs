using SmartSolutionsLab.Yumney.Shared.Common;

namespace SmartSolutionsLab.Yumney.MealPlan.Domain.WeeklyPlan;

/// <summary>
/// A single meal slot within a weekly plan.
/// Supports four content types: Empty, Recipe, Leftover, Freetext.
/// </summary>
public sealed class MealSlot : Entity<MealSlotIdentifier>
{
    public DayOfWeek Day { get; private set; }

    public MealType MealType { get; private set; }

    public SlotContentType ContentType { get; private set; }

    public SlotRecipeReference? Recipe { get; private set; }

    public SlotServings Servings { get; private set; } = default!;

    public FreetextLabel? FreetextLabel { get; private set; }

    public LeftoverLabel? LeftoverLabel { get; private set; }

    public DayOfWeek? LeftoverSourceDay { get; private set; }

    public MealType? LeftoverSourceMealType { get; private set; }

    public MealState State { get; private set; }

    public bool IsEmpty => ContentType == SlotContentType.Empty;

    private MealSlot()
    {
    }

    internal static MealSlot Create(DayOfWeek day, MealType mealType, int defaultServings)
    {
        return new MealSlot
        {
            Id = MealSlotIdentifier.New(),
            Day = day,
            MealType = mealType,
            ContentType = SlotContentType.Empty,
            Servings = SlotServings.From(defaultServings),
        };
    }

    internal MealSlot AssignRecipe(SlotRecipeReference recipe, SlotServings? servings = null)
    {
        ContentType = SlotContentType.Recipe;
        Recipe = recipe;
        FreetextLabel = null;
        LeftoverLabel = null;
        LeftoverSourceDay = null;
        LeftoverSourceMealType = null;
        if (servings is not null)
        {
            Servings = servings;
        }

        return this;
    }

    internal MealSlot SetAsFreetext(FreetextLabel label)
    {
        ContentType = SlotContentType.Freetext;
        FreetextLabel = label;
        Recipe = null;
        LeftoverLabel = null;
        LeftoverSourceDay = null;
        LeftoverSourceMealType = null;
        return this;
    }

    internal MealSlot SetAsLeftover(DayOfWeek sourceDay, MealType sourceMealType, string sourceRecipeTitle, SlotServings? servings = null)
    {
        ContentType = SlotContentType.Leftover;
        LeftoverSourceDay = sourceDay;
        LeftoverSourceMealType = sourceMealType;
        LeftoverLabel = Domain.WeeklyPlan.LeftoverLabel.ForRecipe(sourceRecipeTitle);
        Recipe = null;
        FreetextLabel = null;
        if (servings is not null)
        {
            Servings = servings;
        }

        return this;
    }

    internal MealSlot ClearSlot()
    {
        ContentType = SlotContentType.Empty;
        Recipe = null;
        FreetextLabel = null;
        LeftoverLabel = null;
        LeftoverSourceDay = null;
        LeftoverSourceMealType = null;
        return this;
    }

    internal MealSlot AdjustServingsTo(SlotServings servings)
    {
        Servings = servings;
        return this;
    }

    internal MealSlot MarkAsCooked()
    {
        State = MealState.Cooked;
        return this;
    }

    internal MealSlot MarkAsSkipped()
    {
        State = MealState.Skipped;
        return this;
    }

    internal MealSlot ResetToPlanned()
    {
        State = MealState.Planned;
        return this;
    }

    internal SlotSnapshot TakeSnapshot()
    {
        return new SlotSnapshot(ContentType, Recipe, Servings, FreetextLabel, LeftoverLabel, LeftoverSourceDay, LeftoverSourceMealType, State);
    }

    internal MealSlot RestoreFromSnapshot(SlotSnapshot snapshot)
    {
        ContentType = snapshot.ContentType;
        Recipe = snapshot.Recipe;
        Servings = snapshot.Servings;
        FreetextLabel = snapshot.FreetextLabel;
        LeftoverLabel = snapshot.LeftoverLabel;
        LeftoverSourceDay = snapshot.LeftoverSourceDay;
        LeftoverSourceMealType = snapshot.LeftoverSourceMealType;
        State = snapshot.State;
        return this;
    }

    internal sealed record SlotSnapshot(
        SlotContentType ContentType,
        SlotRecipeReference? Recipe,
        SlotServings Servings,
        FreetextLabel? FreetextLabel,
        LeftoverLabel? LeftoverLabel,
        DayOfWeek? LeftoverSourceDay,
        MealType? LeftoverSourceMealType,
        MealState State);
}
