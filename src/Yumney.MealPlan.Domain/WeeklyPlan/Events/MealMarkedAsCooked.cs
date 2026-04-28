using SmartSolutionsLab.Yumney.Shared.Common;

namespace SmartSolutionsLab.Yumney.MealPlan.Domain.WeeklyPlan.Events;

public sealed record MealMarkedAsCooked(
	DayOfWeek Day,
	MealType MealType,
	SlotRecipeReference? Recipe,
	SlotServings Servings,
	IReadOnlyList<CookedIngredient> Ingredients) : DomainEvent;
