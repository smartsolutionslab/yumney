namespace SmartSolutionsLab.Yumney.Shared.Common;

/// <summary>
/// Resolves default quantities for items added without explicit amounts.
/// Category-based lookup with a fallback of 1 piece for unknown items.
/// </summary>
public static class DefaultQuantityResolver
{
    private static readonly Dictionary<string, ResolvedQuantity> ItemDefaults =
        new(StringComparer.OrdinalIgnoreCase)
        {
            // Liquids → 1L
            ["milk"] = new(1, "L"),
            ["cream"] = new(1, "L"),
            ["juice"] = new(1, "L"),
            ["water"] = new(1, "L"),
            ["broth"] = new(1, "L"),
            ["stock"] = new(1, "L"),
            ["oil"] = new(0.5m, "L"),
            ["olive oil"] = new(0.5m, "L"),
            ["vegetable oil"] = new(0.5m, "L"),

            // Eggs → 6
            ["eggs"] = new(6, "pc"),
            ["egg"] = new(6, "pc"),

            // Dairy → 250g
            ["butter"] = new(250, "g"),
            ["cheese"] = new(250, "g"),
            ["yogurt"] = new(500, "g"),
            ["sour cream"] = new(200, "g"),

            // Meat / Fish → 500g
            ["chicken"] = new(500, "g"),
            ["beef"] = new(500, "g"),
            ["pork"] = new(500, "g"),
            ["ground beef"] = new(500, "g"),
            ["salmon"] = new(500, "g"),
            ["fish"] = new(500, "g"),
            ["shrimp"] = new(500, "g"),

            // Baking → common amounts
            ["flour"] = new(1, "kg"),
            ["sugar"] = new(1, "kg"),
            ["baking powder"] = new(1, "pc"),
            ["yeast"] = new(1, "pc"),

            // Staples → 1 piece or pack
            ["salt"] = new(1, "pc"),
            ["pepper"] = new(1, "pc"),
            ["rice"] = new(1, "kg"),
            ["pasta"] = new(500, "g"),
            ["bread"] = new(1, "pc"),
        };

    private static readonly Dictionary<string, ResolvedQuantity> CategoryDefaults =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["liquid"] = new(1, "L"),
            ["dairy"] = new(250, "g"),
            ["meat"] = new(500, "g"),
            ["fish"] = new(500, "g"),
            ["vegetable"] = new(1, "pc"),
            ["fruit"] = new(1, "pc"),
            ["baking"] = new(1, "pc"),
            ["household"] = new(1, "pc"),
        };

    /// <summary>
    /// Resolve the default quantity for an item. Returns the standard default
    /// for known items, or falls back to 1 piece for unknown items.
    /// </summary>
    public static ResolvedQuantity Resolve(string itemName, string? category = null)
    {
        if (string.IsNullOrWhiteSpace(itemName))
            return ResolvedQuantity.OnePiece;

        var trimmed = itemName.Trim();

        if (ItemDefaults.TryGetValue(trimmed, out var itemDefault))
            return itemDefault;

        if (category is not null && CategoryDefaults.TryGetValue(category, out var categoryDefault))
            return categoryDefault;

        return ResolvedQuantity.OnePiece;
    }
}
