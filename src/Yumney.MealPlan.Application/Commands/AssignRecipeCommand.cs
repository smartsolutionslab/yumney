using SmartSolutionsLab.Yumney.MealPlan.Application.DTOs;
using SmartSolutionsLab.Yumney.MealPlan.Domain.WeeklyPlan;
using SmartSolutionsLab.Yumney.Shared.CQRS;
using SmartSolutionsLab.Yumney.Shared.Outcomes;

namespace SmartSolutionsLab.Yumney.MealPlan.Application.Commands;

public sealed record AssignRecipeCommand(
	WeekIdentifier Week,
	DayOfWeek Day,
	SlotRecipeReference Recipe,
	MealType MealType = MealType.Dinner,
	SlotServings? Servings = null) : ICommand<Result<WeeklyPlanDto>>;
