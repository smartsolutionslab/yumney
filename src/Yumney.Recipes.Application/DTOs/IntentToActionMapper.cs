namespace SmartSolutionsLab.Yumney.Recipes.Application.DTOs;

internal static class IntentToActionMapper
{
	// Phase 1: only navigate is wired. open_recipe / start_cook_mode shapes are
	// defined on ChatActionDto but are populated by Phase 2 (SK function calling),
	// because they require a resolved recipe identifier the intent parser does
	// not produce from a free-text message.
	public static IReadOnlyList<ChatActionDto> Map(ParsedIntentDto intent)
	{
		if (intent.Intent == "navigate" && intent.Entities.TryGetValue("target", out var target))
		{
			var route = ResolveNavigationRoute(target);
			if (route is not null) return [new ChatActionDto(ChatActionType.Navigate, Route: route)];
		}

		return [];
	}

	private static string? ResolveNavigationRoute(string target) => target switch
	{
		"shopping-list" => "/shopping",
		"meal-planner" => "/meal-planner",
		"recipes" => "/recipes",
		"settings" => "/account",
		_ => null,
	};
}
