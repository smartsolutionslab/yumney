using SmartSolutionsLab.Yumney.MealPlan.Application.DTOs;
using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shared.CQRS;

namespace SmartSolutionsLab.Yumney.MealPlan.Application.Commands;

public sealed record ToggleExtendedModeCommand(
    int Year,
    int WeekNumber,
    bool Enable) : ICommand<Result<WeeklyPlanDto>>;
