using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingList;

namespace SmartSolutionsLab.Yumney.Shopping.Application.Common;

#pragma warning disable SA1311
public static class DefaultQuantityResolver
{
	public static readonly Quantity OnePiece = Q(1, "pc");

	private static readonly Dictionary<string, Quantity> itemDefaults = BuildItemDefaults();

	private static readonly Dictionary<string, Quantity> categoryDefaults =
		new(StringComparer.OrdinalIgnoreCase)
		{
			["liquid"] = Q(1, "L"),
			["dairy"] = Q(250, "g"),
			["meat"] = Q(500, "g"),
			["fish"] = Q(500, "g"),
			["vegetable"] = Q(1, "pc"),
			["fruit"] = Q(1, "pc"),
			["baking"] = Q(1, "pc"),
			["household"] = Q(1, "pc"),
		};

	public static Quantity Resolve(string itemName, string? category = null)
	{
		if (string.IsNullOrWhiteSpace(itemName))
		{
			return OnePiece;
		}

		var normalized = Normalize(itemName);

		if (itemDefaults.TryGetValue(normalized, out var itemDefault))
		{
			return itemDefault;
		}

		if (category is not null && categoryDefaults.TryGetValue(category, out var categoryDefault))
		{
			return categoryDefault;
		}

		return OnePiece;
	}

#pragma warning disable SA1204
	private static Quantity Q(decimal amount, string unit) =>
		Quantity.Of(Amount.From(amount), Unit.FromNullable(unit));

	private static string Normalize(string input)
	{
		var trimmed = input.Trim().ToLowerInvariant();

		// Basic English plural → singular (trailing 's' removal for simple cases)
		if (trimmed.Length > 3 && trimmed.EndsWith('s') && !trimmed.EndsWith("ss", StringComparison.Ordinal))
		{
			var singular = trimmed[..^1];
			if (itemDefaults.ContainsKey(singular)) return singular;
		}

		return trimmed;
	}

#pragma warning disable SA1117 // Parameters should be on same line or each on its own line
	private static Dictionary<string, Quantity> BuildItemDefaults()
	{
		var defaults = new Dictionary<string, Quantity>(StringComparer.OrdinalIgnoreCase);

		// Liquids → 1L
		AddWithAliases(defaults, Q(1, "L"),
			"milk", "milch",
			"cream", "sahne", "schlagsahne",
			"juice", "saft",
			"water", "wasser",
			"broth", "bruehe", "brühe",
			"stock", "fond");
		AddWithAliases(defaults, Q(0.5m, "L"),
			"oil", "oel", "öl",
			"olive oil", "olivenoel", "olivenöl",
			"vegetable oil", "pflanzenoel", "pflanzenöl");

		// Eggs → 6
		AddWithAliases(defaults, Q(6, "pc"),
			"egg", "eggs", "ei", "eier");

		// Dairy → 250g
		AddWithAliases(defaults, Q(250, "g"),
			"butter",
			"cheese", "kaese", "käse");
		AddWithAliases(defaults, Q(500, "g"),
			"yogurt", "joghurt");
		AddWithAliases(defaults, Q(200, "g"),
			"sour cream", "schmand", "saure sahne");

		// Meat / Fish → 500g
		AddWithAliases(defaults, Q(500, "g"),
			"chicken", "haehnchen", "hähnchen", "huhn",
			"beef", "rindfleisch", "rind",
			"pork", "schweinefleisch", "schwein",
			"ground beef", "hackfleisch", "gehacktes",
			"salmon", "lachs",
			"fish", "fisch",
			"shrimp", "garnelen", "garnele");

		// Baking
		AddWithAliases(defaults, Q(1, "kg"),
			"flour", "mehl",
			"sugar", "zucker");
		AddWithAliases(defaults, Q(1, "pc"),
			"baking powder", "backpulver",
			"yeast", "hefe");

		// Staples
		AddWithAliases(defaults, Q(1, "pc"),
			"salt", "salz",
			"pepper", "pfeffer",
			"bread", "brot");
		AddWithAliases(defaults, Q(1, "kg"),
			"rice", "reis");
		AddWithAliases(defaults, Q(500, "g"),
			"pasta", "nudeln");

		// Common vegetables
		AddWithAliases(defaults, Q(1, "pc"),
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

	private static void AddWithAliases(Dictionary<string, Quantity> dict, Quantity quantity, params string[] names)
	{
		foreach (var name in names)
		{
			dict.TryAdd(name, quantity);
		}
	}
}
#pragma warning restore SA1311
