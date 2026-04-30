using SmartSolutionsLab.Yumney.Recipes.Application.DTOs;
using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shared.CQRS;

namespace SmartSolutionsLab.Yumney.Recipes.Application.Queries;

/// <summary>
/// LLM-generated recipe suggestions tailored to the user's available
/// ingredients (at-home + staples) and dietary profile (US-343).
/// Triggered by the UI when collection matching ("What Can I Cook?",
/// US-342) returns no or few results. Output uses the same
/// <see cref="ExtractedRecipeDto"/> shape as URL extraction so suggestions
/// flow into the existing save command unchanged.
/// </summary>
public sealed record GetRecipeSuggestionsQuery(int Count = 4)
	: IQuery<Result<IReadOnlyList<ExtractedRecipeDto>>>;
