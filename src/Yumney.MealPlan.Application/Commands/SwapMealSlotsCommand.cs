using SmartSolutionsLab.Yumney.MealPlan.Application.DTOs;
using SmartSolutionsLab.Yumney.MealPlan.Domain.WeeklyPlan;
using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shared.CQRS;

namespace SmartSolutionsLab.Yumney.MealPlan.Application.Commands;

public sealed record SwapMealSlotsCommand(
    WeekIdentifier Week,
    DayOfWeek SourceDay,
    DayOfWeek TargetDay,
    MealType MealType = MealType.Dinner) : ICommand<Result<WeeklyPlanDto>>;
