namespace SmartSolutionsLab.Yumney.Shared.Common;

/// <summary>
/// Rounds quantities up to practical purchase amounts for display.
/// Exact values are preserved in the ledger — rounding is display-only.
/// </summary>
public static class QuantityRounder
{
    /// <summary>
    /// Round a quantity up to the next practical purchase amount.
    /// </summary>
    /// <param name="quantity">The exact calculated quantity.</param>
    /// <param name="unit">The unit (affects rounding strategy).</param>
    /// <returns>The rounded display quantity and the original exact value.</returns>
    public static RoundedQuantity RoundUp(decimal quantity, string? unit)
    {
        if (quantity <= 0)
            return new RoundedQuantity(quantity, quantity);

        var rounded = Math.Ceiling(quantity);

        if (rounded == quantity)
            return new RoundedQuantity(quantity, quantity);

        return new RoundedQuantity(rounded, quantity);
    }
}
