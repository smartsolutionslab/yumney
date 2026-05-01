namespace SmartSolutionsLab.Yumney.Recipes.Application.DTOs;

public sealed record DietaryProfileSnapshot(
	string? DietaryType,
	IReadOnlyList<string> Restrictions)
{
	public static readonly DietaryProfileSnapshot Empty = new(null, []);
}
