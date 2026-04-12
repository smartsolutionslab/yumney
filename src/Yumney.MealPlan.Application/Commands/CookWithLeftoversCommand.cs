using SmartSolutionsLab.Yumney.MealPlan.Application.DTOs;
using SmartSolutionsLab.Yumney.MealPlan.Domain.WeeklyPlan;
using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shared.CQRS;

namespace SmartSolutionsLab.Yumney.MealPlan.Application.Commands;

/// <summary>
/// Assign a recipe with extra servings and create a linked leftover slot on another day.
/// Shopping list calculates for total servings, leftover slot generates no shopping items.
/// </summary>
public sealed record CookWithLeftoversCommand(
    int Year,
    int WeekNumber,
    DayOfWeek CookDay,
    Guid RecipeIdentifier,
    string RecipeTitle,
    int TotalServings,
    int EatServings,
    DayOfWeek LeftoverDay,
    MealType MealType = MealType.Dinner) : ICommand<Result<WeeklyPlanDto>>;
