using SmartSolutionsLab.Yumney.MealPlan.Application.DTOs;
using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shared.CQRS;

namespace SmartSolutionsLab.Yumney.MealPlan.Application.Queries;

/// <summary>
/// Get all recipes planned for a week — only Recipe slots, not Leftover/Freetext.
/// </summary>
public sealed record GetPlannedRecipesQuery(int Year, int WeekNumber) : IQuery<Result<WeeklyPlannedRecipesDto>>;
