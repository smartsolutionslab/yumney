using SmartSolutionsLab.Yumney.Recipes.Application.DTOs;
using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shared.CQRS;

namespace SmartSolutionsLab.Yumney.Recipes.Application.Queries;

/// <summary>
/// "What Can I Cook?" — returns recipes the user can prepare right now,
/// optionally including near-matches (one or two missing ingredients).
/// Powered by the shared <see cref="IIngredientBalanceProvider"/> which
/// merges at-home ledger items with the user's staples list.
/// </summary>
public sealed record GetCookableRecipesQuery(bool FullMatchOnly = false)
	: IQuery<Result<IReadOnlyList<CookableRecipeDto>>>;
