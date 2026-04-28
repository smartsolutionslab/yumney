namespace SmartSolutionsLab.Yumney.MealPlan.Infrastructure.Persistence.ReadModel;

/// <summary>
/// Materialized read model for a single meal plan slot.
/// One row per (OwnerId, Week, Day, MealType).
/// </summary>
public sealed class MealPlanSlotReadItem
{
	public Guid Id { get; set; }

	public string OwnerId { get; set; } = default!;

	public string Week { get; set; } = default!;

	public string Day { get; set; } = default!;

	public string MealType { get; set; } = default!;

	public string ContentType { get; set; } = default!;

	public Guid? RecipeIdentifier { get; set; }

	public string? RecipeTitle { get; set; }

	public int Servings { get; set; }

	public string? FreetextLabel { get; set; }

	public string? LeftoverLabel { get; set; }

	public string? LeftoverSourceDay { get; set; }

	public string? LeftoverSourceMealType { get; set; }

	public string State { get; set; } = default!;

	public DateTime LastUpdated { get; set; }
}
