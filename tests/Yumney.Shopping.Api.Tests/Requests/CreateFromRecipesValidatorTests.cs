using FluentAssertions;
using Xunit;

namespace SmartSolutionsLab.Yumney.Shopping.Api.Tests.Requests;

public class CreateFromRecipesValidatorTests
{
	private readonly Api.Requests.Validator.CreateFromRecipesValidator validator = new();

	[Fact]
	public void Validate_ValidRequest_IsValid()
	{
		var result = validator.Validate(ValidRequest());

		result.IsValid.Should().BeTrue();
	}

	[Theory]
	[InlineData(null)]
	[InlineData("")]
	public void Validate_EmptyTitle_IsInvalid(string? title)
	{
		var request = ValidRequest() with { Title = title! };

		var result = validator.Validate(request);

		result.IsValid.Should().BeFalse();
	}

	[Fact]
	public void Validate_NoRecipes_IsInvalid()
	{
		var request = ValidRequest() with { Recipes = [] };

		var result = validator.Validate(request);

		result.IsValid.Should().BeFalse();
	}

	[Fact]
	public void Validate_RecipeWithEmptyIdentifier_IsInvalid()
	{
		var request = ValidRequest() with
		{
			Recipes = [new Api.Requests.RecipeForList(Guid.Empty, 4)],
		};

		var result = validator.Validate(request);

		result.IsValid.Should().BeFalse();
	}

	[Theory]
	[InlineData(0)]
	[InlineData(-1)]
	public void Validate_RecipeServingsNotPositive_IsInvalid(int servings)
	{
		var request = ValidRequest() with
		{
			Recipes = [new Api.Requests.RecipeForList(Guid.NewGuid(), servings)],
		};

		var result = validator.Validate(request);

		result.IsValid.Should().BeFalse();
	}

	[Fact]
	public void Validate_RecipeServingsNull_IsValid()
	{
		var request = ValidRequest() with
		{
			Recipes = [new Api.Requests.RecipeForList(Guid.NewGuid(), null)],
		};

		var result = validator.Validate(request);

		result.IsValid.Should().BeTrue();
	}

	private static Api.Requests.CreateFromRecipes ValidRequest() => new(
		"Meal Prep",
		[new Api.Requests.RecipeForList(Guid.NewGuid(), 4)]);
}
