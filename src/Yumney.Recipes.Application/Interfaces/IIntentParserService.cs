using SmartSolutionsLab.Yumney.Recipes.Application.DTOs;
using SmartSolutionsLab.Yumney.Shared.Common;

namespace SmartSolutionsLab.Yumney.Recipes.Application.Interfaces;

public interface IIntentParserService
{
    Task<Result<ParsedIntentDto>> ParseAsync(
        string userInput,
        string? pageContext,
        CancellationToken cancellationToken = default);
}
