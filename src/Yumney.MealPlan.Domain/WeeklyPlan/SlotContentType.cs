namespace SmartSolutionsLab.Yumney.MealPlan.Domain.WeeklyPlan;

public enum SlotContentType
{
	/// <summary>The slot has no content assigned.</summary>
	Empty,

	/// <summary>The slot references a saved recipe.</summary>
	Recipe,

	/// <summary>The slot reuses leftovers from another meal.</summary>
	Leftover,

	/// <summary>The slot contains a free-text description.</summary>
	Freetext,
}
