using SmartSolutionsLab.Yumney.MealPlan.Application.DTOs;
using SmartSolutionsLab.Yumney.MealPlan.Domain.WeeklyPlan;
using SmartSolutionsLab.Yumney.Shared.CQRS;
using SmartSolutionsLab.Yumney.Shared.Outcomes;

namespace SmartSolutionsLab.Yumney.MealPlan.Application.Commands;

public sealed record AdjustSlotServingsCommand(
	WeekIdentifier Week,
	DayOfWeek Day,
	MealType MealType,
	SlotServings Servings) : ICommand<Result<WeeklyPlanDto>>;
