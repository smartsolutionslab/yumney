namespace SmartSolutionsLab.Yumney.Recipes.Application.DTOs;

/// <summary>
/// Discriminator for the kind of UI action the chat panel should offer alongside a reply.
/// All actions are client-only directives (interpreted by the Yumney shell) — they are
/// never exposed to external MCP clients.
/// </summary>
public enum ChatActionType
{
	/// <summary>Navigate the SPA to a top-level route (shopping, meal planner, recipes, settings).</summary>
	Navigate,

	/// <summary>Open the recipe detail page for a resolved recipe identifier.</summary>
	OpenRecipe,

	/// <summary>Enter cook mode for a resolved recipe identifier.</summary>
	StartCookMode,
}
