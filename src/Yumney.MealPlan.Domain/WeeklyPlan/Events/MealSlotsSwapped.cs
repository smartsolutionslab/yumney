using SmartSolutionsLab.Yumney.Shared.Abstractions;

namespace SmartSolutionsLab.Yumney.MealPlan.Domain.WeeklyPlan.Events;

public sealed record MealSlotsSwapped(
	DayOfWeek Day1,
	DayOfWeek Day2,
	MealType MealType) : DomainEvent;
