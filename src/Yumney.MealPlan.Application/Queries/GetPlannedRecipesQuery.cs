using SmartSolutionsLab.Yumney.MealPlan.Application.DTOs;
using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shared.CQRS;

namespace SmartSolutionsLab.Yumney.MealPlan.Application.Queries;

public sealed record GetPlannedRecipesQuery(int Year, int WeekNumber) : IQuery<Result<WeeklyPlannedRecipesDto>>;
