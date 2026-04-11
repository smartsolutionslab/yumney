namespace SmartSolutionsLab.Yumney.MealPlan.Application.DTOs;

public sealed record MealSlotDto(string Day, Guid? RecipeIdentifier, string? RecipeTitle, int Servings, bool IsEmpty);

public sealed record WeeklyPlanDto(string Week, IReadOnlyList<MealSlotDto> Slots);
