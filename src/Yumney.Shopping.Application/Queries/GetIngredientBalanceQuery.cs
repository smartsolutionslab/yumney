using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shared.CQRS;
using SmartSolutionsLab.Yumney.Shopping.Application.DTOs;

namespace SmartSolutionsLab.Yumney.Shopping.Application.Queries;

/// <summary>
/// Returns the current ingredient balance ("what's at home right now") for
/// the calling user. Combines ledger-derived at-home items (Bought − Consumed
/// − Removed) with the user's staples list. Powers the "What Can I Cook?"
/// feature (US-342) and any UI that needs an inventory view.
/// </summary>
public sealed record GetIngredientBalanceQuery : IQuery<Result<IngredientBalanceDto>>;
