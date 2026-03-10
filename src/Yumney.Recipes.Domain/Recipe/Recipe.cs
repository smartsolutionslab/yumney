using Yumney.Recipes.Domain.Recipe.Events;
using Yumney.Shared.Common;
using Yumney.Shared.Guards;

namespace Yumney.Recipes.Domain.Recipe;

public sealed class Recipe : AggregateRoot<Guid>
{
    private readonly List<Ingredient> ingredients = [];
    private readonly List<Step> steps = [];

    public RecipeTitle Title { get; private set; } = default!;

    public RecipeDescription? Description { get; private set; }

    public Servings? Servings { get; private set; }

    public PreparationTime? PreparationTime { get; private set; }

    public CookingTime? CookingTime { get; private set; }

    public Difficulty? Difficulty { get; private set; }

    public ImageUrl? ImageUrl { get; private set; }

    public RecipeUrl? SourceUrl { get; private set; }

    public OwnerIdentifier Owner { get; private set; } = default!;

    public DateTime CreatedAt { get; private set; }

    public IReadOnlyList<Ingredient> Ingredients => ingredients.AsReadOnly();

    public IReadOnlyList<Step> Steps => steps.AsReadOnly();

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
        RecipeUrl? sourceUrl = null)
    {
        Ensure.That((IReadOnlyCollection<Ingredient>)ingredients).IsNotEmpty();
        Ensure.That((IReadOnlyCollection<Step>)steps).IsNotEmpty();

        var recipe = new Recipe
        {
            Id = Guid.NewGuid(),
            Title = title,
            SourceUrl = sourceUrl,
            Owner = owner,
            Description = description,
            Servings = servings,
            PreparationTime = preparationTime,
            CookingTime = cookingTime,
            Difficulty = difficulty,
            ImageUrl = imageUrl,
            CreatedAt = DateTime.UtcNow,
        };

        recipe.ingredients.AddRange(ingredients);
        recipe.steps.AddRange(steps);

        recipe.AddDomainEvent(new RecipeSavedEvent(recipe.Id, title));

        return recipe;
    }
}
