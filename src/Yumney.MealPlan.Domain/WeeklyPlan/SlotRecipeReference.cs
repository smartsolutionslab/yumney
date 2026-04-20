using SmartSolutionsLab.Yumney.Shared.Common;

namespace SmartSolutionsLab.Yumney.MealPlan.Domain.WeeklyPlan;

public sealed record SlotRecipeReference : IValueObject
{
	public SlotRecipeIdentifier RecipeIdentifier { get; }

	public SlotRecipeTitle Title { get; }

	private SlotRecipeReference(SlotRecipeIdentifier recipeIdentifier, SlotRecipeTitle title)
	{
		RecipeIdentifier = recipeIdentifier;
		Title = title;
	}

	public static SlotRecipeReference From(SlotRecipeIdentifier recipeIdentifier, SlotRecipeTitle title) => new(recipeIdentifier, title);

	public static SlotRecipeReference From(Guid recipeIdentifier, string title) =>
		From(SlotRecipeIdentifier.From(recipeIdentifier), SlotRecipeTitle.From(title));

	public override string ToString() => $"{Title} ({RecipeIdentifier})";
}
