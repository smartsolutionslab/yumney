using SmartSolutionsLab.Yumney.MealPlan.Application.DTOs;
using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shared.CQRS;

namespace SmartSolutionsLab.Yumney.MealPlan.Application.Queries;

public sealed record SearchMealHistoryQuery(
	string? Term = null,
	int Limit = 20)
	: IQuery<Result<IReadOnlyList<MealHistoryEntryDto>>>;
