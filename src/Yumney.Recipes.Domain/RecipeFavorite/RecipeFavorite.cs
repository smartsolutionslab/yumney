using SmartSolutionsLab.Yumney.Recipes.Domain.Recipe;
using SmartSolutionsLab.Yumney.Shared.Abstractions;
using SmartSolutionsLab.Yumney.Shared.Guards;

namespace SmartSolutionsLab.Yumney.Recipes.Domain.RecipeFavorite;

/// <summary>
/// Records that a specific user has marked a recipe as favorite. The
/// (Owner, RecipeIdentifier) pair is unique. This is its own small
/// aggregate so favoriting/unfavoriting does not load or mutate the
/// full Recipe aggregate.
/// </summary>
public sealed class RecipeFavorite : AggregateRoot<RecipeFavoriteIdentifier>
{
	public RecipeIdentifier RecipeIdentifier { get; private set; } = default!;

	public OwnerIdentifier Owner { get; private set; } = default!;

	public DateTime FavoritedAt { get; private set; }

	private RecipeFavorite()
	{
	}

	public static RecipeFavorite Create(RecipeIdentifier recipe, OwnerIdentifier owner)
	{
		Ensure.That(recipe).IsNotNull();
		Ensure.That(owner).IsNotNull();

		return new RecipeFavorite
		{
			Id = RecipeFavoriteIdentifier.New(),
			RecipeIdentifier = recipe,
			Owner = owner,
			FavoritedAt = DateTime.UtcNow,
		};
	}
}
