using SmartSolutionsLab.Yumney.Shared.Abstractions;

namespace SmartSolutionsLab.Yumney.MealPlan.Domain.WeeklyPlan;

public sealed record SlotRecipeReference : IValueObject
{
	public SlotRecipeIdentifier Identifier { get; }

	public SlotRecipeTitle Title { get; }

	private SlotRecipeReference(SlotRecipeIdentifier recipe, SlotRecipeTitle title)
	{
		Identifier = recipe;
		Title = title;
	}

	public static SlotRecipeReference From(SlotRecipeIdentifier recipe, SlotRecipeTitle title) => new(recipe, title);

	public static SlotRecipeReference From(Guid recipe, string title) =>
		From(SlotRecipeIdentifier.From(recipe), SlotRecipeTitle.From(title));

	public override string ToString() => $"{Title} ({Identifier})";
}
