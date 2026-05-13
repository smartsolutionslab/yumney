using SmartSolutionsLab.Yumney.Recipes.Domain.Recipe;
using RecipeAggregate = SmartSolutionsLab.Yumney.Recipes.Domain.Recipe.Recipe;

namespace SmartSolutionsLab.Yumney.Recipes.Domain.Tests.Builders;

public sealed class RecipeBuilder
{
	private readonly List<Ingredient> ingredients = [];
	private readonly List<Step> steps = [];
	private readonly List<RecipeTag> tags = [];
	private RecipeTitle title = RecipeTitleBuilder.A();
	private OwnerIdentifier owner = OwnerIdentifierBuilder.A();
	private RecipeDescription? description;
	private Servings? servings;
	private TimingInfo? timing;
	private Difficulty? difficulty;
	private ImageUrl? imageUrl;
	private RecipeLanguage? language;
	private RecipeUrl? sourceUrl;

	public static RecipeBuilder A() => new();

	public RecipeBuilder WithTitle(string value) => WithTitle(RecipeTitle.From(value));

	public RecipeBuilder WithTitle(RecipeTitle value)
	{
		title = value;
		return this;
	}

	public RecipeBuilder OwnedBy(string ownerId) => OwnedBy(OwnerIdentifier.From(ownerId));

	public RecipeBuilder OwnedBy(OwnerIdentifier value)
	{
		owner = value;
		return this;
	}

	public RecipeBuilder WithIngredient(string name, decimal? amount = null, string? unit = null)
	{
		var builder = IngredientBuilder.A().Named(name);
		if (amount.HasValue) builder.WithQuantity(amount.Value, Unit.FromNullable(unit));
		ingredients.Add(builder);
		return this;
	}

	public RecipeBuilder WithIngredient(Ingredient ingredient)
	{
		ingredients.Add(ingredient);
		return this;
	}

	public RecipeBuilder WithIngredients(IReadOnlyList<Ingredient> values)
	{
		ingredients.Clear();
		ingredients.AddRange(values);
		return this;
	}

	public RecipeBuilder WithStep(string description)
	{
		steps.Add(StepBuilder.A().Numbered(steps.Count + 1).WithDescription(description));
		return this;
	}

	public RecipeBuilder WithSteps(IReadOnlyList<Step> values)
	{
		steps.Clear();
		steps.AddRange(values);
		return this;
	}

	public RecipeBuilder WithStep(Step step)
	{
		steps.Add(step);
		return this;
	}

	public RecipeBuilder WithDescription(string value)
	{
		description = RecipeDescriptionBuilder.A().With(value);
		return this;
	}

	public RecipeBuilder WithServings(int value)
	{
		servings = ServingsBuilder.A().With(value);
		return this;
	}

	public RecipeBuilder WithTiming(int prepMinutes, int cookMinutes)
	{
		timing = TimingInfoBuilder.A().WithPreparationMinutes(prepMinutes).WithCookingMinutes(cookMinutes);
		return this;
	}

	public RecipeBuilder WithDifficulty(string value)
	{
		difficulty = DifficultyBuilder.A().With(value);
		return this;
	}

	public RecipeBuilder WithImageUrl(string url)
	{
		imageUrl = ImageUrlBuilder.A().With(url);
		return this;
	}

	public RecipeBuilder WithLanguage(string code)
	{
		language = RecipeLanguageBuilder.A().With(code);
		return this;
	}

	public RecipeBuilder WithSourceUrl(string url)
	{
		sourceUrl = RecipeUrlBuilder.A().With(url);
		return this;
	}

	public RecipeBuilder WithTag(string value)
	{
		tags.Add(RecipeTagBuilder.A().With(value));
		return this;
	}

	public RecipeBuilder WithTags(IReadOnlyList<RecipeTag> values)
	{
		tags.Clear();
		tags.AddRange(values);
		return this;
	}

	public RecipeAggregate Build()
	{
		if (ingredients.Count == 0)
		{
			ingredients.Add(IngredientBuilder.A());
		}

		if (steps.Count == 0)
		{
			steps.Add(StepBuilder.A());
		}

		return RecipeAggregate.Create(
			title,
			owner,
			ingredients,
			steps,
			description,
			servings,
			timing,
			difficulty,
			imageUrl,
			language,
			sourceUrl,
			tags.Count == 0 ? null : tags);
	}
}
