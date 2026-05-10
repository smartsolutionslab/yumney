namespace SmartSolutionsLab.Yumney.Recipes.Extraction.Services;

/// <summary>
/// Per-request collector that SK kernel functions write to so the chat service
/// can derive recipe suggestions and chat actions after the LLM completes.
/// Registered as scoped — fresh state per chat turn.
/// </summary>
public sealed class ChatToolContext
{
	private readonly List<RecipeMatch> matches = [];

	/// <summary>Gets recipes referenced by tool calls during the current chat turn.</summary>
	public IReadOnlyList<RecipeMatch> Matches => matches;

	/// <summary>Gets a value indicating whether a "what can I cook?" tool was called — drives the StartCookMode action.</summary>
	public bool ProposeStartCookMode { get; private set; }

	/// <summary>Append a recipe the LLM picked up via a tool call. Deduplicates by identifier (first wins).</summary>
	/// <param name="identifier">Recipe identifier returned by the underlying handler.</param>
	/// <param name="title">Recipe title for downstream display.</param>
	/// <param name="reason">Optional human-readable hint (e.g. "Ready to cook", "Missing: guanciale").</param>
	public void AppendRecipeMatch(Guid identifier, string title, string? reason = null)
	{
		if (matches.Any(match => match.Identifier == identifier)) return;
		matches.Add(new RecipeMatch(identifier, title, reason));
	}

	/// <summary>Flag the current turn as a cookable query so the chat service emits a Start Cooking action.</summary>
	public void MarkCookableQuery() => ProposeStartCookMode = true;

	/// <summary>One recipe surfaced by a chat tool call.</summary>
	public sealed record RecipeMatch(Guid Identifier, string Title, string? Reason);
}
