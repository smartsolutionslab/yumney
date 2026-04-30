using SmartSolutionsLab.Yumney.MealPlan.Application.DTOs;
using SmartSolutionsLab.Yumney.MealPlan.Domain.WeeklyPlan;
using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shared.CQRS;

namespace SmartSolutionsLab.Yumney.MealPlan.Application.Commands;

/// <summary>
/// "Copy to current week" (US-331). Replays the source week's slot
/// assignments onto the target week as <c>Planned</c> — cooked / skipped
/// state is intentionally NOT carried over (this is a fresh plan).
/// </summary>
public sealed record CopyPlanToWeekCommand(WeekIdentifier SourceWeek, WeekIdentifier TargetWeek)
	: ICommand<Result<WeeklyPlanDto>>;
