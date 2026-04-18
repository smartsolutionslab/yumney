namespace SmartSolutionsLab.Yumney.Shared.Common;

/// <summary>
/// A display quantity with the rounded value and the original exact value.
/// </summary>
/// <param name="DisplayQuantity">The rounded-up quantity for display.</param>
/// <param name="ExactQuantity">The original calculated quantity.</param>
public sealed record RoundedQuantity(decimal DisplayQuantity, decimal ExactQuantity)
{
	/// <summary>
	/// Gets a value indicating whether the quantity was rounded.
	/// </summary>
	public bool WasRounded => DisplayQuantity != ExactQuantity;
}
