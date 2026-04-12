namespace SmartSolutionsLab.Yumney.Users.Application.DTOs;

public sealed record UserProfileDto(
    string DisplayName,
    string PreferredLanguage,
    string PreferredUnitSystem,
    int DefaultServings,
    DietaryProfileDto DietaryProfile);

public sealed record DietaryProfileDto(
    string? DietaryType,
    IReadOnlyList<string> Restrictions,
    int? MinVeggieMeals,
    int? MaxRedMeatMeals,
    string? CookingEffort);
