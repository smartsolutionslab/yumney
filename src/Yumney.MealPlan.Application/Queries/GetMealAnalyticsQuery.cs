using SmartSolutionsLab.Yumney.MealPlan.Application.DTOs;
using SmartSolutionsLab.Yumney.Shared.CQRS;
using SmartSolutionsLab.Yumney.Shared.Outcomes;

namespace SmartSolutionsLab.Yumney.MealPlan.Application.Queries;

public sealed record GetMealAnalyticsQuery(int Year, int? Month) : IQuery<Result<MealAnalyticsDto>>;
