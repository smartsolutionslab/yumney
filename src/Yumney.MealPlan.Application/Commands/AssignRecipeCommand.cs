using SmartSolutionsLab.Yumney.MealPlan.Application.DTOs;
using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shared.CQRS;

namespace SmartSolutionsLab.Yumney.MealPlan.Application.Commands;

public sealed record AssignRecipeCommand(
    int Year,
    int WeekNumber,
    DayOfWeek Day,
    Guid RecipeIdentifier,
    string RecipeTitle,
    int? Servings) : ICommand<Result<WeeklyPlanDto>>;
