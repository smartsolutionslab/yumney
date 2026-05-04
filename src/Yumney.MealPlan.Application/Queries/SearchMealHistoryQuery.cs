using SmartSolutionsLab.Yumney.MealPlan.Application.DTOs;
using SmartSolutionsLab.Yumney.Shared.CQRS;
using SmartSolutionsLab.Yumney.Shared.Outcomes;

namespace SmartSolutionsLab.Yumney.MealPlan.Application.Queries;

public sealed record SearchMealHistoryQuery(
	string? Term = null,
	int Limit = 20)
	: IQuery<Result<IReadOnlyList<MealHistoryEntryDto>>>;
