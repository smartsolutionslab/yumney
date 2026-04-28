using SmartSolutionsLab.Yumney.Shared.Common;

namespace SmartSolutionsLab.Yumney.MealPlan.Domain.WeeklyPlan.Events;

public sealed record RecipeAssigned(
	DayOfWeek Day,
	MealType MealType,
	SlotRecipeReference Recipe,
	SlotServings? Servings) : DomainEvent;
