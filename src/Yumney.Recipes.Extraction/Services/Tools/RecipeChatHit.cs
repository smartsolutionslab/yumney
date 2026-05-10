namespace SmartSolutionsLab.Yumney.Recipes.Extraction.Services.Tools;

/// <summary>
/// Lean recipe shape returned to the LLM by chat tools. Trimmed to identifier,
/// title, and a short description so the model can mention recipes in its reply
/// without bloating the context with full recipe details.
/// </summary>
/// <param name="Identifier">Recipe identifier.</param>
/// <param name="Title">Recipe title.</param>
/// <param name="Description">Short description, if any.</param>
public sealed record RecipeChatHit(Guid Identifier, string Title, string? Description);

/// <summary>
/// Result row returned by the cookable-recipes tool. Adds match tier + missing
/// ingredients so the LLM can describe whether the user can fully cook now or
/// whether anything's missing.
/// </summary>
/// <param name="Identifier">Recipe identifier.</param>
/// <param name="Title">Recipe title.</param>
/// <param name="Tier">Cookability tier — "Full" or "Near".</param>
/// <param name="MissingIngredients">Ingredients the user is missing for this recipe.</param>
public sealed record CookableRecipeChatHit(
	Guid Identifier,
	string Title,
	string Tier,
	IReadOnlyList<string> MissingIngredients);
