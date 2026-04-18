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

    public Guid? RecipeIdentifier { get; private set; }

    public string? RecipeTitle { get; private set; }

    public int Servings { get; private set; }

    public string? FreetextLabel { get; private set; }

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
            Servings = defaultServings,
        };
    }

    internal MealSlot AssignRecipe(Guid recipeIdentifier, string recipeTitle, int? servings = null)
    {
        ContentType = SlotContentType.Recipe;
        RecipeIdentifier = recipeIdentifier;
        RecipeTitle = recipeTitle;
        FreetextLabel = null;
        LeftoverSourceDay = null;
        LeftoverSourceMealType = null;
        if (servings.HasValue)
            Servings = servings.Value;
        return this;
    }

    internal MealSlot SetAsFreetext(string label)
    {
        ContentType = SlotContentType.Freetext;
        FreetextLabel = label;
        RecipeIdentifier = null;
        RecipeTitle = null;
        LeftoverSourceDay = null;
        LeftoverSourceMealType = null;
        return this;
    }

    internal MealSlot SetAsLeftover(DayOfWeek sourceDay, MealType sourceMealType, string sourceRecipeTitle, int? servings = null)
    {
        ContentType = SlotContentType.Leftover;
        LeftoverSourceDay = sourceDay;
        LeftoverSourceMealType = sourceMealType;
        RecipeTitle = $"Leftovers: {sourceRecipeTitle}";
        RecipeIdentifier = null;
        FreetextLabel = null;
        if (servings.HasValue)
            Servings = servings.Value;
        return this;
    }

    internal MealSlot ClearSlot()
    {
        ContentType = SlotContentType.Empty;
        RecipeIdentifier = null;
        RecipeTitle = null;
        FreetextLabel = null;
        LeftoverSourceDay = null;
        LeftoverSourceMealType = null;
        return this;
    }

    internal MealSlot AdjustServingsTo(int servings)
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
        return new SlotSnapshot(ContentType, RecipeIdentifier, RecipeTitle, Servings, FreetextLabel, LeftoverSourceDay, LeftoverSourceMealType, State);
    }

    internal MealSlot RestoreFromSnapshot(SlotSnapshot snapshot)
    {
        ContentType = snapshot.ContentType;
        RecipeIdentifier = snapshot.RecipeIdentifier;
        RecipeTitle = snapshot.RecipeTitle;
        Servings = snapshot.Servings;
        FreetextLabel = snapshot.FreetextLabel;
        LeftoverSourceDay = snapshot.LeftoverSourceDay;
        LeftoverSourceMealType = snapshot.LeftoverSourceMealType;
        State = snapshot.State;
        return this;
    }

    internal sealed record SlotSnapshot(
        SlotContentType ContentType,
        Guid? RecipeIdentifier,
        string? RecipeTitle,
        int Servings,
        string? FreetextLabel,
        DayOfWeek? LeftoverSourceDay,
        MealType? LeftoverSourceMealType,
        MealState State);
}
