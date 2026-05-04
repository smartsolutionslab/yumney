using SmartSolutionsLab.Yumney.Shared.Abstractions;

namespace SmartSolutionsLab.Yumney.MealPlan.Domain.WeeklyPlan.Events;

public sealed record MealSetAsFreetext(
	DayOfWeek Day,
	MealType MealType,
	FreetextLabel Label) : DomainEvent;
