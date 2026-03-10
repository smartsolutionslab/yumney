namespace Yumney.Recipes.Application.DTOs;

public sealed record SavedRecipeDto(Guid Identifier, string Title, DateTime ImportedAt);
