namespace SmartSolutionsLab.Yumney.Shared.AI;

/// <summary>
/// Task roles that may run on their own model. Services inject the matching
/// keyed client, e.g. <c>[FromKeyedServices(LlmTasks.Fast)] IChatClient</c>;
/// everything else uses the default (unkeyed) client.
/// </summary>
public static class LlmTasks
{
	/// <summary>Short, high-volume classification (categorization, intent parsing) — suits a cheaper model.</summary>
	public const string Fast = "fast";

	/// <summary>Image understanding (photo import, ingredient recognition) — needs a vision-capable model.</summary>
	public const string Vision = "vision";

	/// <summary>Gets all task roles.</summary>
	public static IReadOnlyList<string> All { get; } = [Fast, Vision];
}
