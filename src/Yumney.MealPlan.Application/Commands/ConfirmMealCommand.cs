using SmartSolutionsLab.Yumney.MealPlan.Application.DTOs;
using SmartSolutionsLab.Yumney.MealPlan.Domain.WeeklyPlan;
using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shared.CQRS;

namespace SmartSolutionsLab.Yumney.MealPlan.Application.Commands;

public sealed record ConfirmMealCommand(
    int Year,
    int WeekNumber,
    DayOfWeek Day,
    MealType MealType,
    MealState NewState) : ICommand<Result<WeeklyPlanDto>>;
