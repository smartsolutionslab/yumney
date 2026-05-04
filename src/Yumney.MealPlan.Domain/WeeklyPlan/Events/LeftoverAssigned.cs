using SmartSolutionsLab.Yumney.Shared.Abstractions;

namespace SmartSolutionsLab.Yumney.MealPlan.Domain.WeeklyPlan.Events;

public sealed record LeftoverAssigned(
	DayOfWeek Day,
	MealType MealType,
	DayOfWeek SourceDay,
	MealType SourceMealType,
	SlotRecipeTitle SourceRecipeTitle,
	SlotServings? Servings) : DomainEvent;
