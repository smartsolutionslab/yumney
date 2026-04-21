using SmartSolutionsLab.Yumney.Recipes.Domain.Recipe;

namespace SmartSolutionsLab.Yumney.Recipes.Application.DTOs;

/// <summary>
/// Output of a scrape. <see cref="StructuredRecipe"/> is set when the page
/// exposes a machine-readable recipe (schema.org/Recipe JSON-LD) and can be
/// used directly; <see cref="CleanedText"/> is only consumed by the LLM
/// fallback path when <see cref="StructuredRecipe"/> is null.
/// </summary>
public sealed record ScrapedContent(
	string CleanedText,
	RecipeUrl? SourceUrl,
	ExtractedRecipeDto? StructuredRecipe = null);
