using FluentAssertions;
using SmartSolutionsLab.Yumney.TestBuilders.Users;
using SmartSolutionsLab.Yumney.Users.Application.DTOs;
using SmartSolutionsLab.Yumney.Users.Domain.UserActivity;
using Xunit;

namespace SmartSolutionsLab.Yumney.Users.Application.Tests.DTOs;

public class UserActivityMappingExtensionsTests
{
	[Fact]
	public void ToDto_FullActivity_MapsAllFields()
	{
		var recipeId = Guid.NewGuid();
		var activity = UserActivity.Record(
			OwnerIdentifier.From("kc-user-1"),
			ActivityType.RecipeViewed,
			RecipeIdentifierSnapshot.From(recipeId),
			RecipeTitleSnapshot.From("Pasta"));

		var dto = activity.ToDto();

		dto.Type.Should().Be("recipe_viewed");
		dto.RecipeIdentifier.Should().Be(recipeId);
		dto.RecipeTitle.Should().Be("Pasta");
		dto.OccurredAt.Should().Be(activity.OccurredAt);
	}

	[Fact]
	public void ToDto_DeletedRecipeActivity_MapsNullTitle()
	{
		var recipeId = Guid.NewGuid();
		var activity = UserActivity.Record(
			OwnerIdentifier.From("kc-user-1"),
			ActivityType.RecipeDeleted,
			RecipeIdentifierSnapshot.From(recipeId));

		var dto = activity.ToDto();

		dto.RecipeIdentifier.Should().Be(recipeId);
		dto.RecipeTitle.Should().BeNull();
	}

	[Fact]
	public void ToDtos_Collection_ReturnsListPreservingOrder()
	{
		var a = UserActivity.Record(
			OwnerIdentifier.From("kc-user-1"),
			ActivityType.RecipeImported,
			RecipeIdentifierSnapshot.From(Guid.NewGuid()),
			RecipeTitleSnapshot.From("A"));
		var b = UserActivity.Record(
			OwnerIdentifier.From("kc-user-1"),
			ActivityType.RecipeCooked,
			RecipeIdentifierSnapshot.From(Guid.NewGuid()),
			RecipeTitleSnapshot.From("B"));

		var dtos = new[] { a, b }.ToDtos();

		dtos.Should().HaveCount(2);
		dtos[0].RecipeTitle.Should().Be("A");
		dtos[1].RecipeTitle.Should().Be("B");
	}

	[Fact]
	public void ToDtos_EmptyCollection_ReturnsEmptyList()
	{
		Array.Empty<UserActivity>().ToDtos().Should().BeEmpty();
	}

	[Fact]
	public void RecipeActivityStats_ToDto_MapsAllFields()
	{
		var lastCookedAt = new DateTime(2026, 4, 15, 19, 30, 0, DateTimeKind.Utc);
		var stats = new RecipeActivityStats(CookCount: 3, LastCookedAt: lastCookedAt, ViewCount: 7);

		var dto = stats.ToDto();

		dto.CookCount.Should().Be(3);
		dto.LastCookedAt.Should().Be(lastCookedAt);
		dto.ViewCount.Should().Be(7);
	}

	[Fact]
	public void RecipeActivityStats_ToDto_NeverCooked_PreservesNullLastCookedAt()
	{
		var stats = new RecipeActivityStats(CookCount: 0, LastCookedAt: null, ViewCount: 5);

		var dto = stats.ToDto();

		dto.CookCount.Should().Be(0);
		dto.LastCookedAt.Should().BeNull();
		dto.ViewCount.Should().Be(5);
	}
}
