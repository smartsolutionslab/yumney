using SmartSolutionsLab.Yumney.Recipes.Domain.Recipe;
using RecipeAggregate = SmartSolutionsLab.Yumney.Recipes.Domain.Recipe.Recipe;

namespace SmartSolutionsLab.Yumney.Recipes.Domain.Tests.Builders;

public sealed class RecipeBuilder
{
	private readonly List<Ingredient> ingredients = [];
	private readonly List<Step> steps = [];
	private readonly List<RecipeTag> tags = [];
	private RecipeTitle title = RecipeTitle.From("Test Recipe");
	private OwnerIdentifier owner = OwnerIdentifier.From("user-123");
	private RecipeDescription? description;
	private Servings? servings;
	private TimingInfo? timing;
	private Difficulty? difficulty;
	private ImageUrl? imageUrl;
	private RecipeLanguage? language;
	private RecipeUrl? sourceUrl;

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
		var quantity = Quantity.FromNullable(Amount.FromNullable(amount), Unit.FromNullable(unit));
		ingredients.Add(Ingredient.Create(IngredientName.From(name), quantity));
		return this;
	}

	public RecipeBuilder WithIngredient(Ingredient ingredient)
	{
		ingredients.Add(ingredient);
		return this;
	}

	public RecipeBuilder WithStep(string description)
	{
		steps.Add(Step.Create(StepNumber.From(steps.Count + 1), StepDescription.From(description)));
		return this;
	}

	public RecipeBuilder WithStep(Step step)
	{
		steps.Add(step);
		return this;
	}

	public RecipeBuilder WithDescription(string value)
	{
		description = RecipeDescription.From(value);
		return this;
	}

	public RecipeBuilder WithServings(int value)
	{
		servings = Servings.From(value);
		return this;
	}

	public RecipeBuilder WithTiming(int prepMinutes, int cookMinutes)
	{
		timing = TimingInfo.Of(PreparationTime.From(prepMinutes), CookingTime.From(cookMinutes));
		return this;
	}

	public RecipeBuilder WithDifficulty(string value)
	{
		difficulty = Difficulty.From(value);
		return this;
	}

	public RecipeBuilder WithImageUrl(string url)
	{
		imageUrl = ImageUrl.From(url);
		return this;
	}

	public RecipeBuilder WithLanguage(string code)
	{
		language = RecipeLanguage.From(code);
		return this;
	}

	public RecipeBuilder WithSourceUrl(string url)
	{
		sourceUrl = RecipeUrl.From(url);
		return this;
	}

	public RecipeBuilder WithTag(string value)
	{
		tags.Add(RecipeTag.From(value));
		return this;
	}

	public RecipeAggregate Build()
	{
		if (ingredients.Count == 0)
		{
			ingredients.Add(Ingredient.Create(IngredientName.From("Flour"), null));
		}

		if (steps.Count == 0)
		{
			steps.Add(Step.Create(StepNumber.From(1), StepDescription.From("Mix")));
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
