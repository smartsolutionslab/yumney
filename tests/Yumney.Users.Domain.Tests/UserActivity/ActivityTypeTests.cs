using FluentAssertions;
using SmartSolutionsLab.Yumney.Shared.Guards;
using SmartSolutionsLab.Yumney.Users.Domain.UserActivity;
using Xunit;

namespace SmartSolutionsLab.Yumney.Users.Domain.Tests.UserActivity;

public class ActivityTypeTests
{
	[Theory]
	[InlineData("recipe_imported")]
	[InlineData("recipe_viewed")]
	[InlineData("recipe_cooked")]
	[InlineData("recipe_edited")]
	[InlineData("recipe_deleted")]
	[InlineData("shopping_list_created")]
	public void From_AllowedValue_CreatesInstance(string value)
	{
		var type = ActivityType.From(value);

		type.Value.Should().Be(value);
	}

	[Fact]
	public void From_InvalidValue_ThrowsGuardException()
	{
		var act = () => ActivityType.From("invalid_type");

		act.Should().Throw<GuardException>();
	}

	[Theory]
	[InlineData(null)]
	[InlineData("")]
	[InlineData("   ")]
	public void From_NullOrWhitespace_ThrowsGuardException(string? value)
	{
		var act = () => ActivityType.From(value!);

		act.Should().Throw<GuardException>();
	}

	[Fact]
	public void StaticInstances_HaveCorrectValues()
	{
		ActivityType.RecipeImported.Value.Should().Be("recipe_imported");
		ActivityType.RecipeViewed.Value.Should().Be("recipe_viewed");
		ActivityType.RecipeCooked.Value.Should().Be("recipe_cooked");
		ActivityType.RecipeEdited.Value.Should().Be("recipe_edited");
		ActivityType.RecipeDeleted.Value.Should().Be("recipe_deleted");
		ActivityType.ShoppingListCreated.Value.Should().Be("shopping_list_created");
	}

	[Fact]
	public void ImplicitConversion_ReturnsValue()
	{
		string value = ActivityType.RecipeImported;

		value.Should().Be("recipe_imported");
	}

	[Fact]
	public void Equality_SameValue_AreEqual()
	{
		var a = ActivityType.From("recipe_imported");
		var b = ActivityType.RecipeImported;

		a.Should().Be(b);
	}
}
