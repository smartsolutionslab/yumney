namespace Yumney.Recipes.Application.DTOs;

public sealed record ExtractedRecipeDto
{
    public required string Title { get; init; }

    public string? Description { get; init; }

    public required IReadOnlyList<ExtractedIngredientDto> Ingredients { get; init; }

    public required IReadOnlyList<ExtractedStepDto> Steps { get; init; }

    public int? Servings { get; init; }

    public int? PrepTimeMinutes { get; init; }

    public int? CookTimeMinutes { get; init; }

    public string? Difficulty { get; init; }

    public string? ImageUrl { get; init; }
}

public sealed record ExtractedIngredientDto(string Name, decimal? Amount, string? Unit);

public sealed record ExtractedStepDto(int Number, string Description);
