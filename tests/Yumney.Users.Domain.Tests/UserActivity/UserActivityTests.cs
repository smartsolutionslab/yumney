using FluentAssertions;
using SmartSolutionsLab.Yumney.Users.Domain.UserActivity;
using Xunit;

namespace SmartSolutionsLab.Yumney.Users.Domain.Tests.UserActivity;

public class UserActivityTests
{
	[Fact]
	public void Record_CreatesActivityWithCorrectProperties()
	{
		var owner = OwnerIdentifier.From("user-123");
		var recipeId = RecipeIdentifierSnapshot.From(Guid.NewGuid());
		var recipeTitle = RecipeTitleSnapshot.From("Pasta");

		var activity = Domain.UserActivity.UserActivity.Record(
			owner,
			ActivityType.RecipeImported,
			recipeId,
			recipeTitle);

		activity.Owner.Should().Be(owner);
		activity.Type.Should().Be(ActivityType.RecipeImported);
		activity.RecipeIdentifier.Should().Be(recipeId);
		activity.RecipeTitle.Should().Be(recipeTitle);
		activity.OccurredAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
	}

	[Fact]
	public void Record_WithoutOptionalParams_SetsNulls()
	{
		var owner = OwnerIdentifier.From("user-456");

		var activity = Domain.UserActivity.UserActivity.Record(owner, ActivityType.RecipeViewed);

		activity.RecipeIdentifier.Should().BeNull();
		activity.RecipeTitle.Should().BeNull();
	}

	[Fact]
	public void Record_GeneratesUniqueIdentifier()
	{
		var owner = OwnerIdentifier.From("user-123");

		var a = Domain.UserActivity.UserActivity.Record(owner, ActivityType.RecipeImported);
		var b = Domain.UserActivity.UserActivity.Record(owner, ActivityType.RecipeImported);

		a.Id.Should().NotBe(b.Id);
	}
}
