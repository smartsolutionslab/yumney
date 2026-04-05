namespace SmartSolutionsLab.Yumney.Users.Application.DTOs;

public sealed record UserActivityDto(
    string Type,
    Guid? RecipeIdentifier,
    string? RecipeTitle,
    DateTime OccurredAt);
