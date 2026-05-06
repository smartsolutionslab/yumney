using SmartSolutionsLab.Yumney.MealPlan.Application.DTOs;
using SmartSolutionsLab.Yumney.Shared.CQRS;
using SmartSolutionsLab.Yumney.Shared.Outcomes;
using SmartSolutionsLab.Yumney.Shared.Paging;

namespace SmartSolutionsLab.Yumney.MealPlan.Application.Queries;

public sealed record SearchMealHistoryQuery(
	PagingOptions Paging,
	string? Term = null)
	: IQuery<Result<PagedResult<MealHistoryEntryDto>>>;
