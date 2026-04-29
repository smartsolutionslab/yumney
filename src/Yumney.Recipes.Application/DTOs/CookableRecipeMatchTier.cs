namespace SmartSolutionsLab.Yumney.Recipes.Application.DTOs;

/// <summary>
/// Classification of a recipe against a user's at-home + staples balance.
/// </summary>
public enum CookableRecipeMatchTier
{
	/// <summary>All ingredients are available — recipe is ready to cook.</summary>
	Full,

	/// <summary>One or two ingredients are missing — close to ready.</summary>
	Near,
}
