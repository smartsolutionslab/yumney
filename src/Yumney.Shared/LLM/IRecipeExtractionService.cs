namespace Yumney.Shared.LLM;

public interface IRecipeExtractionService
{
    Task<ExtractedRecipeDto> ExtractRecipeAsync(
        string htmlContent,
        string sourceUrl,
        CancellationToken cancellationToken = default);
}

public record ExtractedRecipeDto(
    string Title,
    string? Description,
    int Servings,
    int? PreparationTimeMinutes,
    string? Difficulty,
    string? Language,
    string? ImageUrl,
    IReadOnlyList<ExtractedIngredientDto> Ingredients,
    IReadOnlyList<ExtractedStepDto> Steps);

public record ExtractedIngredientDto(
    decimal? Amount,
    string? Unit,
    string Name);

public record ExtractedStepDto(
    int StepNumber,
    string Instruction,
    int? DurationMinutes);
