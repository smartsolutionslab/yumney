using SmartSolutionsLab.Yumney.Recipes.Application.DTOs;
using SmartSolutionsLab.Yumney.Recipes.Application.Interfaces;
using SmartSolutionsLab.Yumney.Shared.Outcomes;

namespace SmartSolutionsLab.Yumney.Recipes.Extraction.TestStubs;

/// <summary>
/// E2E / integration test stub. Recognises a small fixed set of navigate
/// phrases deterministically so chat-action tests can assert the Actions
/// channel without needing a real LLM. Falls back to <c>general_chat</c> for
/// every other input — that's the same behaviour the previous version had
/// for all inputs.
/// </summary>
internal sealed class StubIntentParserService : IIntentParserService
{
	public Task<Result<ParsedIntentDto>> ParseAsync(string userInput, string? pageContext, CancellationToken cancellationToken = default)
	{
		var target = ResolveNavigateTarget(userInput);
		if (target is not null)
		{
			return Task.FromResult(Result<ParsedIntentDto>.Success(new ParsedIntentDto(
				"navigate",
				new Dictionary<string, string> { ["target"] = target },
				null)));
		}

		return Task.FromResult(Result<ParsedIntentDto>.Success(new ParsedIntentDto("general_chat", [], null)));
	}

	private static string? ResolveNavigateTarget(string input)
	{
		var normalized = input.Trim().ToLowerInvariant();
		if (normalized.Contains("shopping list", StringComparison.Ordinal)) return "shopping-list";
		if (normalized.Contains("meal planner", StringComparison.Ordinal)) return "meal-planner";
		if (normalized.Contains("settings", StringComparison.Ordinal)) return "settings";
		if (normalized.Contains("recipes page", StringComparison.Ordinal) || normalized.Contains("open recipes", StringComparison.Ordinal)) return "recipes";
		return null;
	}
}
