namespace SmartSolutionsLab.Yumney.MealPlan.Application.DTOs;

public sealed record MealSlotDto(string Day, string MealType, Guid? RecipeIdentifier, string? RecipeTitle, int Servings, bool IsEmpty);

public sealed record WeeklyPlanDto(string Week, bool IsExtendedMode, IReadOnlyList<MealSlotDto> Slots);
