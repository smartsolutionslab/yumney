using SmartSolutionsLab.Yumney.Shared.Common;

namespace SmartSolutionsLab.Yumney.Users.Domain.UserActivity;

public sealed class UserActivity : Entity<UserActivityIdentifier>
{
	public OwnerIdentifier Owner { get; private set; } = default!;

	public ActivityType Type { get; private set; } = default!;

	public RecipeIdentifierSnapshot? RecipeIdentifier { get; private set; }

	public RecipeTitleSnapshot? RecipeTitle { get; private set; }

	public DateTime OccurredAt { get; private set; }

	private UserActivity()
	{
	}

	public static UserActivity Record(
		OwnerIdentifier owner,
		ActivityType type,
		RecipeIdentifierSnapshot? recipeIdentifier = null,
		RecipeTitleSnapshot? recipeTitle = null)
	{
		return new UserActivity
		{
			Id = UserActivityIdentifier.New(),
			Owner = owner,
			Type = type,
			RecipeIdentifier = recipeIdentifier,
			RecipeTitle = recipeTitle,
			OccurredAt = DateTime.UtcNow,
		};
	}
}
