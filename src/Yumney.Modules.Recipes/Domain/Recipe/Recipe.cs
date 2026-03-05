using Yumney.Modules.Recipes.Domain.Ingredient;
using Yumney.Modules.Recipes.Domain.Recipe.Events;
using Yumney.Shared.Common;
using Yumney.Shared.Guards;

namespace Yumney.Modules.Recipes.Domain.Recipe;

public class Recipe : AggregateRoot<RecipeId>
{
    private readonly List<RecipeIngredient> _ingredients = [];
    private readonly List<RecipeStep> _steps = [];
    private readonly List<string> _tags = [];

    private Recipe()
    {
    }

    public Guid UserId { get; private set; }

    public RecipeTitle Title { get; private set; } = null!;

    public RecipeDescription? Description { get; private set; }

    public SourceUrl? Source { get; private set; }

    public Servings OriginalServings { get; private set; } = null!;

    public PreparationTime? PrepTime { get; private set; }

    public Difficulty Difficulty { get; private set; }

    public string? ImageUrl { get; private set; }

    public string Language { get; private set; } = "de";

    public bool IsFavorite { get; private set; }

    public int? Rating { get; private set; }

    public string? Notes { get; private set; }

    public DateTime CreatedAt { get; private set; }

    public DateTime UpdatedAt { get; private set; }

    public IReadOnlyList<RecipeIngredient> Ingredients => _ingredients.AsReadOnly();

    public IReadOnlyList<RecipeStep> Steps => _steps.AsReadOnly();

    public IReadOnlyList<string> Tags => _tags.AsReadOnly();

    public static Recipe ImportedFrom(
        SourceUrl source,
        Guid userId,
        RecipeTitle title,
        Servings servings)
    {
        Ensure.That(source).IsNotNull();
        Ensure.That(title).IsNotNull();
        Ensure.That(servings).IsNotNull();

        var recipe = new Recipe
        {
            Id = RecipeId.New(),
            UserId = userId,
            Title = title,
            Source = source,
            OriginalServings = servings,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };

        recipe.AddDomainEvent(new RecipeImportedEvent(recipe.Id, title, source));

        return recipe;
    }

    public static Recipe CreatedManually(
        Guid userId,
        RecipeTitle title,
        Servings servings)
    {
        Ensure.That(title).IsNotNull();
        Ensure.That(servings).IsNotNull();

        return new Recipe
        {
            Id = RecipeId.New(),
            UserId = userId,
            Title = title,
            OriginalServings = servings,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };
    }

    public void RenameAs(RecipeTitle newTitle)
    {
        Ensure.That(newTitle).IsNotNull();
        Title = newTitle;
        UpdatedAt = DateTime.UtcNow;
    }

    public void DescribeAs(RecipeDescription? description)
    {
        Description = description;
        UpdatedAt = DateTime.UtcNow;
    }

    public void AdjustServingsTo(Servings newServings)
    {
        Ensure.That(newServings).IsNotNull();
        OriginalServings = newServings;
        UpdatedAt = DateTime.UtcNow;
    }

    public void AddIngredient(IngredientName name, Quantity quantity, int sortOrder)
    {
        Ensure.That(name).IsNotNull();
        Ensure.That(quantity).IsNotNull();

        var ingredient = new RecipeIngredient(
            Guid.NewGuid(),
            Id,
            name,
            quantity,
            sortOrder);

        _ingredients.Add(ingredient);
        UpdatedAt = DateTime.UtcNow;
    }

    public void RemoveIngredient(Guid ingredientId)
    {
        var ingredient = _ingredients.FirstOrDefault(i => i.Id == ingredientId);
        if (ingredient is not null)
        {
            _ingredients.Remove(ingredient);
            UpdatedAt = DateTime.UtcNow;
        }
    }

    public void AddStep(string instruction, int stepNumber, int? durationMinutes = null)
    {
        Ensure.That(instruction).IsNotNullOrWhiteSpace();

        var step = new RecipeStep(
            Guid.NewGuid(),
            Id,
            stepNumber,
            instruction,
            durationMinutes);

        _steps.Add(step);
        UpdatedAt = DateTime.UtcNow;
    }

    public void ToggleFavorite()
    {
        IsFavorite = !IsFavorite;
        UpdatedAt = DateTime.UtcNow;
    }

    public void RateAs(int rating)
    {
        Ensure.That(rating).IsInRange(1, 5);
        Rating = rating;
        UpdatedAt = DateTime.UtcNow;
    }

    public void AddNote(string? notes)
    {
        Notes = notes;
        UpdatedAt = DateTime.UtcNow;
    }

    public void TagWith(string tag)
    {
        Ensure.That(tag).IsNotNullOrWhiteSpace();
        if (!_tags.Contains(tag, StringComparer.OrdinalIgnoreCase))
        {
            _tags.Add(tag.Trim());
            UpdatedAt = DateTime.UtcNow;
        }
    }

    public void RemoveTag(string tag)
    {
        _tags.RemoveAll(t => t.Equals(tag, StringComparison.OrdinalIgnoreCase));
        UpdatedAt = DateTime.UtcNow;
    }
}
