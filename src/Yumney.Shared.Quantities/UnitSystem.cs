namespace SmartSolutionsLab.Yumney.Shared.Quantities;

/// <summary>
/// The unit system the user reads ingredients in. Stored on the user's
/// profile (US-100); applied as the default on recipe detail (US-125).
/// </summary>
public enum UnitSystem
{
	/// <summary>Grams, kilograms, millilitres, litres, °C — the SI/cooking-metric stack used by the EU and most of the world.</summary>
	Metric,

	/// <summary>Ounces, pounds, fluid ounces, cups, tablespoons, °F — the US-customary stack used by most American cookbooks.</summary>
	Imperial,
}
