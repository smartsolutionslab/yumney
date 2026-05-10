using SmartSolutionsLab.Yumney.Recipes.Application.DTOs;

namespace SmartSolutionsLab.Yumney.Recipes.Extraction.Services;

internal static class ChatToolContextMappingExtensions
{
	public static ChatRecipeSuggestionDto ToSuggestion(this ChatToolContext.RecipeMatch match) =>
		new(match.Identifier, match.Title, match.Reason);

	public static IReadOnlyList<ChatRecipeSuggestionDto> ToSuggestions(this IEnumerable<ChatToolContext.RecipeMatch> matches) =>
		[.. matches.Select(match => match.ToSuggestion())];
}
