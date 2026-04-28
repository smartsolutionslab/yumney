namespace SmartSolutionsLab.Yumney.MealPlan.Domain.WeeklyPlan;

public sealed record MealSlot(
	DayOfWeek Day,
	MealType MealType,
	SlotContentType ContentType,
	SlotRecipeReference? Recipe,
	SlotServings Servings,
	FreetextLabel? FreetextLabel,
	LeftoverLabel? LeftoverLabel,
	DayOfWeek? LeftoverSourceDay,
	MealType? LeftoverSourceMealType,
	MealState State)
{
	public bool IsEmpty => ContentType == SlotContentType.Empty;

	public static MealSlot Empty(DayOfWeek day, MealType mealType, SlotServings servings) =>
		new(day, mealType, SlotContentType.Empty, null, servings, null, null, null, null, MealState.Planned);

	internal MealSlot WithRecipe(SlotRecipeReference recipe, SlotServings? servings) =>
		this with
		{
			ContentType = SlotContentType.Recipe,
			Recipe = recipe,
			FreetextLabel = null,
			LeftoverLabel = null,
			LeftoverSourceDay = null,
			LeftoverSourceMealType = null,
			Servings = servings ?? Servings,
		};

	internal MealSlot WithFreetext(FreetextLabel label) =>
		this with
		{
			ContentType = SlotContentType.Freetext,
			FreetextLabel = label,
			Recipe = null,
			LeftoverLabel = null,
			LeftoverSourceDay = null,
			LeftoverSourceMealType = null,
		};

	internal MealSlot WithLeftover(
		DayOfWeek sourceDay,
		MealType sourceMealType,
		SlotRecipeTitle sourceRecipeTitle,
		SlotServings? servings) =>
		this with
		{
			ContentType = SlotContentType.Leftover,
			LeftoverSourceDay = sourceDay,
			LeftoverSourceMealType = sourceMealType,
			LeftoverLabel = LeftoverLabel.ForRecipe(sourceRecipeTitle),
			Recipe = null,
			FreetextLabel = null,
			Servings = servings ?? Servings,
		};

	internal MealSlot Cleared() =>
		this with
		{
			ContentType = SlotContentType.Empty,
			Recipe = null,
			FreetextLabel = null,
			LeftoverLabel = null,
			LeftoverSourceDay = null,
			LeftoverSourceMealType = null,
		};

	internal MealSlot WithServings(SlotServings servings) => this with { Servings = servings };

	internal MealSlot InState(MealState state) => this with { State = state };
}
