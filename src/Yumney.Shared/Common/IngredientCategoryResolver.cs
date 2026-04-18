namespace SmartSolutionsLab.Yumney.Shared.Common;

/// <summary>
/// Resolves ingredient categories from known item names (EN + DE).
/// Returns null for unknown items — callers should fall back to LLM or default to Other.
/// </summary>
public static class IngredientCategoryResolver
{
#pragma warning disable SA1311
	private static readonly Dictionary<string, IngredientCategory> itemCategories = BuildItemCategories();
#pragma warning restore SA1311

	/// <summary>
	/// Try to resolve the category for an item by name.
	/// </summary>
	/// <param name="itemName">The item name (EN or DE).</param>
	/// <returns>The category if known, or null if the item is not recognized.</returns>
	public static IngredientCategory? Resolve(string itemName)
	{
		if (string.IsNullOrWhiteSpace(itemName))
			return null;

		var normalized = itemName.Trim().ToLowerInvariant();

		if (itemCategories.TryGetValue(normalized, out var category))
			return category;

		// Basic English plural normalization
		if (normalized.Length > 3 && normalized.EndsWith('s') && !normalized.EndsWith("ss", StringComparison.Ordinal))
		{
			var singular = normalized[..^1];
			if (itemCategories.TryGetValue(singular, out var singularCategory))
				return singularCategory;
		}

		return null;
	}

#pragma warning disable SA1117
	private static Dictionary<string, IngredientCategory> BuildItemCategories()
	{
		var map = new Dictionary<string, IngredientCategory>(StringComparer.OrdinalIgnoreCase);

		Add(map, IngredientCategory.Produce,
			"onion", "zwiebel", "garlic", "knoblauch",
			"potato", "kartoffel", "tomato", "tomate",
			"carrot", "karotte", "moehre", "möhre",
			"cucumber", "gurke", "lettuce", "salat",
			"pepper", "paprika", "zucchini", "broccoli", "brokkoli",
			"spinach", "spinat", "mushroom", "pilz", "champignon",
			"celery", "sellerie", "leek", "lauch",
			"lemon", "zitrone", "lime", "limette",
			"apple", "apfel", "banana", "banane",
			"orange", "grape", "traube", "berry", "beere",
			"avocado", "ginger", "ingwer", "herbs", "kräuter");

		Add(map, IngredientCategory.Dairy,
			"milk", "milch", "cream", "sahne", "schlagsahne",
			"butter", "cheese", "käse", "kaese",
			"yogurt", "joghurt", "sour cream", "schmand", "saure sahne",
			"quark", "cottage cheese", "mozzarella", "parmesan",
			"egg", "eggs", "ei", "eier");

		Add(map, IngredientCategory.MeatFish,
			"chicken", "hähnchen", "haehnchen", "huhn",
			"beef", "rindfleisch", "rind",
			"pork", "schweinefleisch", "schwein",
			"ground beef", "hackfleisch", "gehacktes",
			"salmon", "lachs", "fish", "fisch",
			"shrimp", "garnelen", "garnele",
			"tuna", "thunfisch", "sausage", "wurst", "bratwurst",
			"bacon", "speck", "ham", "schinken",
			"turkey", "truthahn", "pute", "lamb", "lamm");

		Add(map, IngredientCategory.Bakery,
			"bread", "brot", "rolls", "brötchen", "broetchen",
			"baguette", "croissant", "toast", "pretzel", "brezel");

		Add(map, IngredientCategory.Frozen,
			"ice cream", "eis", "frozen pizza", "tiefkühlpizza",
			"frozen vegetables", "tiefkühlgemüse",
			"fish sticks", "fischstäbchen", "fischstaebchen");

		Add(map, IngredientCategory.Beverages,
			"juice", "saft", "water", "wasser",
			"beer", "bier", "wine", "wein",
			"coffee", "kaffee", "tea", "tee",
			"soda", "limonade", "limo");

		Add(map, IngredientCategory.Pantry,
			"flour", "mehl", "sugar", "zucker",
			"rice", "reis", "pasta", "nudeln",
			"oil", "öl", "oel", "olive oil", "olivenöl", "olivenoel",
			"vegetable oil", "pflanzenöl", "pflanzenoel",
			"salt", "salz", "pepper", "pfeffer",
			"baking powder", "backpulver", "yeast", "hefe",
			"vinegar", "essig", "soy sauce", "sojasoße", "sojasauce",
			"honey", "honig", "mustard", "senf",
			"ketchup", "mayonnaise", "mayo",
			"canned tomatoes", "dosentomaten", "tomato paste", "tomatenmark",
			"broth", "brühe", "bruehe", "stock", "fond",
			"nuts", "nüsse", "nuesse", "almonds", "mandeln",
			"chocolate", "schokolade", "cocoa", "kakao",
			"oats", "haferflocken", "cornstarch", "speisestärke");

		Add(map, IngredientCategory.Household,
			"toilet paper", "toilettenpapier",
			"dish soap", "spülmittel", "spuelmittel",
			"paper towels", "küchentücher", "kuechentuecher",
			"trash bags", "müllbeutel", "muellbeutel",
			"sponge", "schwamm", "aluminum foil", "alufolie",
			"plastic wrap", "frischhaltefolie",
			"laundry detergent", "waschmittel",
			"hand soap", "handseife");

		return map;
	}
#pragma warning restore SA1117

	private static void Add(Dictionary<string, IngredientCategory> dict, IngredientCategory category, params string[] names)
	{
		foreach (var name in names)
			dict.TryAdd(name, category);
	}
}
