namespace SmartSolutionsLab.Yumney.Shared.Common;

/// <summary>
/// Cross-module read access to a user's dietary profile (US-301).
/// Returns the lightweight slice that drives recipe-suggestion prompting:
/// dietary type + restrictions. Implementations: HTTP client for callers
/// outside the Users module.
/// </summary>
public interface IDietaryProfileProvider
{
	Task<DietaryProfileSnapshot> GetAsync(string ownerId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Snapshot of the dietary fields needed for downstream prompt building.
/// </summary>
/// <param name="DietaryType">e.g. omnivore / vegetarian / vegan / pescatarian / flexitarian. Null = no preference set.</param>
/// <param name="Restrictions">free-form restriction tags (gluten-free, lactose-free, nut-allergy, …).</param>
public sealed record DietaryProfileSnapshot(
	string? DietaryType,
	IReadOnlyList<string> Restrictions)
{
	public static readonly DietaryProfileSnapshot Empty = new(null, []);
}
