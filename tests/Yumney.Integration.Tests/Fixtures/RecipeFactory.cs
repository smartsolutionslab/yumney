using SmartSolutionsLab.Yumney.Recipes.Domain.Recipe;
using SmartSolutionsLab.Yumney.TestBuilders.Recipes;

namespace SmartSolutionsLab.Yumney.Integration.Tests.Fixtures;

public static class RecipeFactory
{
	private const string DefaultOwner = "integration-test-user";

	public static Recipe Create(
		string title,
		string? owner = null,
		string? description = null,
		int? servings = null,
		IReadOnlyList<(string Name, decimal? Amount, string? Unit)>? ingredients = null,
		IReadOnlyList<string>? steps = null,
		IReadOnlyList<string>? tags = null)
	{
		var builder = RecipeBuilder.A().WithTitle(title).OwnedBy(owner ?? DefaultOwner);
		if (description is not null) builder.WithDescription(description);
		if (servings is not null) builder.WithServings(servings.Value);

		if (ingredients is not null)
		{
			builder.WithIngredients([.. ingredients.Select(spec =>
			{
				var ingredient = IngredientBuilder.A().Named(spec.Name);
				if (spec.Amount.HasValue) ingredient.WithQuantity(spec.Amount.Value, Unit.FromNullable(spec.Unit));
				return ingredient.Build();
			})]);
		}

		if (steps is not null)
		{
			builder.WithSteps([.. steps.Select((s, i) =>
				StepBuilder.A().Numbered(i + 1).WithDescription(s).Build())]);
		}

		if (tags is not null)
		{
			foreach (var tag in tags) builder.WithTag(tag);
		}

		return builder.Build();
	}

	public static Recipe Lasagne(string? owner = null) => Create(
		"Classic Lasagne",
		owner,
		"Traditional Italian lasagne with rich Bolognese sauce and creamy bechamel",
		servings: 6,
		ingredients:
		[
			("Lasagne sheets", 500m, "g"),
			("Ground beef", 400m, "g"),
			("Onion", 2m, null),
			("Garlic", 3m, "cloves"),
			("Canned tomatoes", 800m, "g"),
			("Butter", 50m, "g"),
			("Flour", 50m, "g"),
			("Milk", 500m, "ml"),
			("Parmesan cheese", 100m, "g"),
			("Mozzarella", 200m, "g")
		],
		steps:
		[
			"Brown the ground beef with onion and garlic",
			"Add canned tomatoes and simmer for 30 minutes",
			"Make bechamel: melt butter, add flour, gradually add milk",
			"Layer lasagne sheets, Bolognese, and bechamel in a baking dish",
			"Top with mozzarella and parmesan, bake at 180°C for 40 minutes"
		],
		tags: ["italian", "pasta", "comfort-food"]);

	public static Recipe TomatoSoup(string? owner = null) => Create(
		"Roasted Tomato Soup",
		owner,
		"Creamy roasted tomato soup with fresh basil",
		servings: 4,
		ingredients:
		[
			("Tomatoes", 1000m, "g"),
			("Onion", 1m, null),
			("Garlic", 4m, "cloves"),
			("Olive oil", 3m, "tbsp"),
			("Fresh basil", 10m, "leaves"),
			("Heavy cream", 100m, "ml")
		],
		steps:
		[
			"Halve tomatoes and roast at 200°C for 25 minutes",
			"Sauté onion and garlic until soft",
			"Blend roasted tomatoes with onion mixture",
			"Stir in cream and fresh basil, season to taste"
		]);

	public static Recipe ChocolateCake(string? owner = null) => Create(
		"Chocolate Fudge Cake",
		owner,
		"Rich and moist chocolate cake with fudge frosting",
		servings: 12,
		ingredients:
		[
			("Flour", 200m, "g"),
			("Cocoa powder", 75m, "g"),
			("Sugar", 300m, "g"),
			("Eggs", 3m, null),
			("Butter", 150m, "g"),
			("Dark chocolate", 200m, "g")
		],
		steps:
		[
			"Mix dry ingredients together",
			"Cream butter and sugar, add eggs",
			"Fold in dry ingredients and melted chocolate",
			"Bake at 170°C for 35 minutes"
		]);
}
