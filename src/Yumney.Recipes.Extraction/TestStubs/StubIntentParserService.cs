using SmartSolutionsLab.Yumney.Recipes.Application.DTOs;
using SmartSolutionsLab.Yumney.Recipes.Application.Interfaces;
using SmartSolutionsLab.Yumney.Shared.Outcomes;

namespace SmartSolutionsLab.Yumney.Recipes.Extraction.TestStubs;

internal sealed class StubIntentParserService : IIntentParserService
{
	public Task<Result<ParsedIntentDto>> ParseAsync(string userInput, string? pageContext, CancellationToken cancellationToken = default) =>
		Task.FromResult(Result<ParsedIntentDto>.Success(new ParsedIntentDto("general_chat", [], null)));
}
