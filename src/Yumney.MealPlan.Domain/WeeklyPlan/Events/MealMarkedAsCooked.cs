using SmartSolutionsLab.Yumney.Shared.Abstractions;

namespace SmartSolutionsLab.Yumney.MealPlan.Domain.WeeklyPlan.Events;

public sealed record MealMarkedAsCooked(
	DayOfWeek Day,
	MealType MealType,
	SlotRecipeReference? Recipe,
	SlotServings Servings,
	IReadOnlyList<CookedIngredient> Ingredients) : DomainEvent;
