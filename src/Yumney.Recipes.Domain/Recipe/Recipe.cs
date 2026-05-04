using SmartSolutionsLab.Yumney.Recipes.Domain.Recipe.Events;
using SmartSolutionsLab.Yumney.Shared.Abstractions;
using SmartSolutionsLab.Yumney.Shared.Guards;

namespace SmartSolutionsLab.Yumney.Recipes.Domain.Recipe;

public sealed class Recipe : AggregateRoot<RecipeIdentifier>
{
	private readonly List<Ingredient> ingredients = [];
	private readonly List<Step> steps = [];
	private readonly List<RecipeTag> tags = [];

	public RecipeTitle Title { get; private set; } = default!;

	public RecipeDescription? Description { get; private set; }

	public Servings? Servings { get; private set; }

	public TimingInfo? Timing { get; private set; }

	public Difficulty? Difficulty { get; private set; }

	public ImageUrl? ImageUrl { get; private set; }

	public RecipeLanguage? Language { get; private set; }

	public RecipeUrl? SourceUrl { get; private set; }

	public OwnerIdentifier Owner { get; private set; } = default!;

	public DateTime CreatedAt { get; private set; }

	public Rating? Rating { get; private set; }

	public Notes? Notes { get; private set; }

	public IReadOnlyList<Ingredient> Ingredients => ingredients.AsReadOnly();

	public IReadOnlyList<Step> Steps => steps.AsReadOnly();

	public IReadOnlyList<RecipeTag> Tags => tags.AsReadOnly();

	private Recipe()
	{
	}

	public static Recipe Create(
		RecipeTitle title,
		OwnerIdentifier owner,
		IReadOnlyList<Ingredient> ingredients,
		IReadOnlyList<Step> steps,
		RecipeDescription? description = null,
		Servings? servings = null,
		TimingInfo? timing = null,
		Difficulty? difficulty = null,
		ImageUrl? imageUrl = null,
		RecipeLanguage? language = null,
		RecipeUrl? sourceUrl = null,
		IReadOnlyList<RecipeTag>? tags = null)
	{
		Ensure.That(ingredients).IsNotEmpty();
		Ensure.That(steps).IsNotEmpty();

		var recipe = new Recipe
		{
			Id = RecipeIdentifier.New(),
			Title = title,
			SourceUrl = sourceUrl,
			Owner = owner,
			Description = description,
			Servings = servings,
			Timing = timing,
			Difficulty = difficulty,
			ImageUrl = imageUrl,
			Language = language,
			CreatedAt = DateTime.UtcNow,
		};

		recipe.ingredients.AddRange(ingredients);
		recipe.steps.AddRange(steps);
		if (tags is not null)
		{
			recipe.tags.AddRange(tags);
		}

		recipe.AddDomainEvent(new RecipeSavedEvent(recipe.Id, title));

		return recipe;
	}

	public Recipe Update(
		RecipeTitle title,
		IReadOnlyList<Ingredient> ingredients,
		IReadOnlyList<Step> steps,
		RecipeDescription? description = null,
		Servings? servings = null,
		TimingInfo? timing = null,
		Difficulty? difficulty = null,
		ImageUrl? imageUrl = null,
		IReadOnlyList<RecipeTag>? tags = null)
	{
		Ensure.That(ingredients).IsNotEmpty();
		Ensure.That(steps).IsNotEmpty();

		Title = title;
		Description = description;
		Servings = servings;
		Timing = timing;
		Difficulty = difficulty;
		ImageUrl = imageUrl;

		this.ingredients.Clear();
		this.ingredients.AddRange(ingredients);

		this.steps.Clear();
		this.steps.AddRange(steps);

		this.tags.Clear();
		if (tags is not null)
		{
			this.tags.AddRange(tags);
		}

		AddDomainEvent(new RecipeUpdatedEvent(Id, title));
		return this;
	}

	public Recipe MarkAsDeleted()
	{
		AddDomainEvent(new RecipeDeletedEvent(Id, Title, Owner));
		return this;
	}

	public Recipe RateAs(Rating rating)
	{
		Rating = rating;
		AddDomainEvent(new RecipeRatedEvent(Id, rating));
		return this;
	}

	public Recipe UpdateNotes(Notes? notes)
	{
		Notes = notes;
		AddDomainEvent(new RecipeNotesUpdatedEvent(Id, notes is not null));
		return this;
	}
}
