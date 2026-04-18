using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shared.Guards;

namespace SmartSolutionsLab.Yumney.MealPlan.Domain.WeeklyPlan;

/// <summary>
/// A reference to a recipe assigned to a meal slot, carrying the identifier and display title.
/// </summary>
public sealed record SlotRecipeReference : IValueObject
{
    public Guid RecipeIdentifier { get; }

    public string Title { get; }

    private SlotRecipeReference(Guid recipeIdentifier, string title)
    {
        Ensure.That(recipeIdentifier).IsNotEmpty();
        string validated = Ensure.That(title).IsNotNullOrWhiteSpace().HasMaxLength(200).AndReturn();
        RecipeIdentifier = recipeIdentifier;
        Title = validated.Trim();
    }

    public static SlotRecipeReference From(Guid recipeIdentifier, string title) => new(recipeIdentifier, title);

    public override string ToString() => $"{Title} ({RecipeIdentifier})";
}
