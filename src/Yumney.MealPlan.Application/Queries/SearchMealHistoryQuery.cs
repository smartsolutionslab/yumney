using SmartSolutionsLab.Yumney.MealPlan.Application.DTOs;
using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shared.CQRS;

namespace SmartSolutionsLab.Yumney.MealPlan.Application.Queries;

/// <summary>
/// "When did I last cook X?" (US-331). Returns cooked-state meal slots whose
/// recipe title matches the term (case-insensitive substring), newest first.
/// Empty term returns the most recent cooked meals across all recipes — useful
/// for the history view's default state.
/// </summary>
public sealed record SearchMealHistoryQuery(string? Term = null, int Limit = 20)
	: IQuery<Result<IReadOnlyList<MealHistoryEntryDto>>>;
