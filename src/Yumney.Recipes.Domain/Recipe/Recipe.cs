using SmartSolutionsLab.Yumney.Recipes.Domain.Recipe.Events;
using SmartSolutionsLab.Yumney.Shared.Common;
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

    public PreparationTime? PreparationTime { get; private set; }

    public CookingTime? CookingTime { get; private set; }

    public Difficulty? Difficulty { get; private set; }

    public ImageUrl? ImageUrl { get; private set; }

    public RecipeLanguage? Language { get; private set; }

    public RecipeUrl? SourceUrl { get; private set; }

    public OwnerIdentifier Owner { get; private set; } = default!;

    public DateTime CreatedAt { get; private set; }

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
        PreparationTime? preparationTime = null,
        CookingTime? cookingTime = null,
        Difficulty? difficulty = null,
        ImageUrl? imageUrl = null,
        RecipeLanguage? language = null,
        RecipeUrl? sourceUrl = null,
        IReadOnlyList<RecipeTag>? tags = null)
    {
        Ensure.That((IReadOnlyCollection<Ingredient>)ingredients).IsNotEmpty();
        Ensure.That((IReadOnlyCollection<Step>)steps).IsNotEmpty();

        var recipe = new Recipe
        {
            Id = RecipeIdentifier.New(),
            Title = title,
            SourceUrl = sourceUrl,
            Owner = owner,
            Description = description,
            Servings = servings,
            PreparationTime = preparationTime,
            CookingTime = cookingTime,
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

    public void Update(
        RecipeTitle title,
        IReadOnlyList<Ingredient> ingredients,
        IReadOnlyList<Step> steps,
        RecipeDescription? description = null,
        Servings? servings = null,
        PreparationTime? preparationTime = null,
        CookingTime? cookingTime = null,
        Difficulty? difficulty = null,
        ImageUrl? imageUrl = null,
        IReadOnlyList<RecipeTag>? tags = null)
    {
        Ensure.That((IReadOnlyCollection<Ingredient>)ingredients).IsNotEmpty();
        Ensure.That((IReadOnlyCollection<Step>)steps).IsNotEmpty();

        Title = title;
        Description = description;
        Servings = servings;
        PreparationTime = preparationTime;
        CookingTime = cookingTime;
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
    }

    public void MarkAsDeleted()
    {
        AddDomainEvent(new RecipeDeletedEvent(Id, Title, Owner));
    }
}
