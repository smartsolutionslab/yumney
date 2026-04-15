namespace SmartSolutionsLab.Yumney.Shared.Common;

/// <summary>
/// Lightweight ingredient info for cross-module use.
/// </summary>
public sealed record RecipeIngredientInfo(string Name, decimal? Amount, string? Unit, int? RecipeServings);
