using SmartSolutionsLab.Yumney.Shared.Abstractions;

namespace SmartSolutionsLab.Yumney.MealPlan.Domain.WeeklyPlan.Events;

public sealed record ServingsAdjusted(
	DayOfWeek Day,
	MealType MealType,
	SlotServings Servings) : DomainEvent;
