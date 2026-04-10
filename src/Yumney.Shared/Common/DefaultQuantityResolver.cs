namespace SmartSolutionsLab.Yumney.Shared.Common;

/// <summary>
/// Resolves default quantities for items added without explicit amounts.
/// Supports EN and DE item names with basic plural normalization.
/// Category-based lookup with a fallback of 1 piece for unknown items.
/// </summary>
#pragma warning disable SA1311
public static class DefaultQuantityResolver
{
    private static readonly Dictionary<string, ResolvedQuantity> itemDefaults =
        BuilditemDefaults();

    private static readonly Dictionary<string, ResolvedQuantity> categoryDefaults =
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
    /// <param name="itemName">The item name (EN or DE).</param>
    /// <param name="category">Optional category hint for unknown items.</param>
    /// <returns>The resolved default quantity.</returns>
    public static ResolvedQuantity Resolve(string itemName, string? category = null)
    {
        if (string.IsNullOrWhiteSpace(itemName))
            return ResolvedQuantity.OnePiece;

        var normalized = Normalize(itemName);

        if (itemDefaults.TryGetValue(normalized, out var itemDefault))
            return itemDefault;

        if (category is not null && categoryDefaults.TryGetValue(category, out var categoryDefault))
            return categoryDefault;

        return ResolvedQuantity.OnePiece;
    }

    private static string Normalize(string input)
    {
        var trimmed = input.Trim().ToLowerInvariant();

        // Basic English plural → singular (trailing 's' removal for simple cases)
        if (trimmed.Length > 3 && trimmed.EndsWith('s') && !trimmed.EndsWith("ss", StringComparison.Ordinal))
        {
            var singular = trimmed[..^1];
            if (itemDefaults.ContainsKey(singular))
                return singular;
        }

        return trimmed;
    }

#pragma warning disable SA1117 // Parameters should be on same line or each on its own line
    private static Dictionary<string, ResolvedQuantity> BuilditemDefaults()
    {
        var defaults = new Dictionary<string, ResolvedQuantity>(StringComparer.OrdinalIgnoreCase);

        // Liquids → 1L
        AddWithAliases(defaults, new(1, "L"),
            "milk", "milch",
            "cream", "sahne", "schlagsahne",
            "juice", "saft",
            "water", "wasser",
            "broth", "bruehe", "brühe",
            "stock", "fond");
        AddWithAliases(defaults, new(0.5m, "L"),
            "oil", "oel", "öl",
            "olive oil", "olivenoel", "olivenöl",
            "vegetable oil", "pflanzenoel", "pflanzenöl");

        // Eggs → 6
        AddWithAliases(defaults, new(6, "pc"),
            "egg", "eggs", "ei", "eier");

        // Dairy → 250g
        AddWithAliases(defaults, new(250, "g"),
            "butter",
            "cheese", "kaese", "käse");
        AddWithAliases(defaults, new(500, "g"),
            "yogurt", "joghurt");
        AddWithAliases(defaults, new(200, "g"),
            "sour cream", "schmand", "saure sahne");

        // Meat / Fish → 500g
        AddWithAliases(defaults, new(500, "g"),
            "chicken", "haehnchen", "hähnchen", "huhn",
            "beef", "rindfleisch", "rind",
            "pork", "schweinefleisch", "schwein",
            "ground beef", "hackfleisch", "gehacktes",
            "salmon", "lachs",
            "fish", "fisch",
            "shrimp", "garnelen", "garnele");

        // Baking
        AddWithAliases(defaults, new(1, "kg"),
            "flour", "mehl",
            "sugar", "zucker");
        AddWithAliases(defaults, new(1, "pc"),
            "baking powder", "backpulver",
            "yeast", "hefe");

        // Staples
        AddWithAliases(defaults, new(1, "pc"),
            "salt", "salz",
            "pepper", "pfeffer",
            "bread", "brot");
        AddWithAliases(defaults, new(1, "kg"),
            "rice", "reis");
        AddWithAliases(defaults, new(500, "g"),
            "pasta", "nudeln");

        // Common vegetables
        AddWithAliases(defaults, new(1, "pc"),
            "onion", "zwiebel",
            "garlic", "knoblauch",
            "potato", "kartoffel",
            "tomato", "tomate",
            "carrot", "karotte", "moehre", "möhre",
            "cucumber", "gurke",
            "lettuce", "salat",
            "lemon", "zitrone");

        return defaults;
    }
#pragma warning restore SA1117

    private static void AddWithAliases(Dictionary<string, ResolvedQuantity> dict, ResolvedQuantity quantity, params string[] names)
    {
        foreach (var name in names)
            dict.TryAdd(name, quantity);
    }
}
#pragma warning restore SA1311
