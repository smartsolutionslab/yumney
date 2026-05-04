namespace SmartSolutionsLab.Yumney.Users.Application.DTOs;

public sealed record RecipeActivityStatsDto(int CookCount, DateTime? LastCookedAt, int ViewCount);
