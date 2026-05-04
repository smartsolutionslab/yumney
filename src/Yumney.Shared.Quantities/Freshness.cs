namespace SmartSolutionsLab.Yumney.Shared.Quantities;

/// <summary>
/// Freshness state for a stored ingredient (US-341). Computed from the
/// item's category-based shelf life and how long ago it was last bought.
/// </summary>
public enum Freshness
{
	/// <summary>Category has no perishability tracking (pantry, household, …).</summary>
	NotTracked,

	/// <summary>Within the first half of the shelf life — green indicator.</summary>
	Fresh,

	/// <summary>Past half the shelf life but not yet expired — yellow indicator.</summary>
	UseSoon,

	/// <summary>At or past the expected shelf life — orange indicator.</summary>
	CheckIt,
}
