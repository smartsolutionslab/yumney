namespace SmartSolutionsLab.Yumney.MealPlan.Application.DTOs;

public sealed record MealSlotDto(
    string Day,
    string MealType,
    string ContentType,
    Guid? RecipeIdentifier,
    string? RecipeTitle,
    int Servings,
    string? FreetextLabel,
    string? LeftoverSourceDay,
    string? LeftoverSourceMealType,
    bool IsEmpty);

public sealed record WeeklyPlanDto(string Week, bool IsExtendedMode, IReadOnlyList<MealSlotDto> Slots);
