using SmartSolutionsLab.Yumney.Shared.Abstractions;

namespace SmartSolutionsLab.Yumney.MealPlan.Domain.WeeklyPlan.Events;

public sealed record RecipeAssigned(
	DayOfWeek Day,
	MealType MealType,
	SlotRecipeReference Recipe,
	SlotServings? Servings) : DomainEvent;
