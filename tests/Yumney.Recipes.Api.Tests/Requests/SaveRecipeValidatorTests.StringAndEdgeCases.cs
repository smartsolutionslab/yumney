using FluentAssertions;
using FluentValidation.TestHelper;
using SmartSolutionsLab.Yumney.Recipes.Api.Requests;
using Xunit;

namespace SmartSolutionsLab.Yumney.Recipes.Api.Tests.Requests;

#pragma warning disable SA1601
public partial class SaveRecipeValidatorTests
#pragma warning restore SA1601
{
	[Fact]
	public void Validate_SourceUrlExceedsMaxLength_HasError()
	{
		var request = CreateValidRequest() with
		{
			SourceUrl = "https://example.com/" + new string('a', 2040),
		};

		var result = validator.TestValidate(request);

		result.ShouldHaveValidationErrorFor(x => x.SourceUrl);
	}

	[Fact]
	public void Validate_IngredientNameExceedsMaxLength_HasError()
	{
		var request = CreateValidRequest() with
		{
			Ingredients = [new SaveRecipeIngredient(new string('a', 201), null, null)],
		};

		var result = validator.TestValidate(request);

		result.IsValid.Should().BeFalse();
	}

	[Fact]
	public void Validate_StepDescriptionExceedsMaxLength_HasError()
	{
		var request = CreateValidRequest() with
		{
			Steps = [new SaveRecipeStep(1, new string('a', 2001))],
		};

		var result = validator.TestValidate(request);

		result.IsValid.Should().BeFalse();
	}

	[Fact]
	public void Validate_DescriptionExceedsMaxLength_HasError()
	{
		var request = CreateValidRequest() with { Description = new string('a', 2001) };

		var result = validator.TestValidate(request);

		result.ShouldHaveValidationErrorFor(x => x.Description);
	}

	[Fact]
	public void Validate_DescriptionAtMaxLength_HasNoError()
	{
		var request = CreateValidRequest() with { Description = new string('a', 2000) };

		var result = validator.TestValidate(request);

		result.ShouldNotHaveValidationErrorFor(x => x.Description);
	}

	[Fact]
	public void Validate_DifficultyExceedsMaxLength_HasError()
	{
		var request = CreateValidRequest() with { Difficulty = new string('a', 51) };

		var result = validator.TestValidate(request);

		result.ShouldHaveValidationErrorFor(x => x.Difficulty);
	}

	[Fact]
	public void Validate_InvalidImageUrl_HasError()
	{
		var request = CreateValidRequest() with { ImageUrl = "not-a-url" };

		var result = validator.TestValidate(request);

		result.ShouldHaveValidationErrorFor(x => x.ImageUrl);
	}

	[Fact]
	public void Validate_ValidImageUrl_HasNoError()
	{
		var request = CreateValidRequest() with { ImageUrl = "https://example.com/image.jpg" };

		var result = validator.TestValidate(request);

		result.ShouldNotHaveValidationErrorFor(x => x.ImageUrl);
	}

	[Fact]
	public void Validate_ImageUrlExceedsMaxLength_HasError()
	{
		var request = CreateValidRequest() with
		{
			ImageUrl = "https://example.com/" + new string('a', 2040),
		};

		var result = validator.TestValidate(request);

		result.ShouldHaveValidationErrorFor(x => x.ImageUrl);
	}

	[Fact]
	public void Validate_EmptyDifficulty_HasError()
	{
		var request = CreateValidRequest() with { Difficulty = string.Empty };

		var result = validator.TestValidate(request);

		result.ShouldHaveValidationErrorFor(x => x.Difficulty);
	}

	[Fact]
	public void Validate_NegativeIngredientAmount_HasError()
	{
		var request = CreateValidRequest() with
		{
			Ingredients = [new SaveRecipeIngredient("Flour", -1, "g")],
		};

		var result = validator.TestValidate(request);

		result.IsValid.Should().BeFalse();
	}

	[Fact]
	public void Validate_ZeroIngredientAmount_HasNoError()
	{
		var request = CreateValidRequest() with
		{
			Ingredients = [new SaveRecipeIngredient("Flour", 0, "g")],
		};

		var result = validator.TestValidate(request);

		result.ShouldNotHaveAnyValidationErrors();
	}

	[Fact]
	public void Validate_ZeroStepNumber_HasError()
	{
		var request = CreateValidRequest() with
		{
			Steps = [new SaveRecipeStep(0, "Cook pasta")],
		};

		var result = validator.TestValidate(request);

		result.IsValid.Should().BeFalse();
	}

	[Fact]
	public void Validate_NegativeStepNumber_HasError()
	{
		var request = CreateValidRequest() with
		{
			Steps = [new SaveRecipeStep(-1, "Cook pasta")],
		};

		var result = validator.TestValidate(request);

		result.IsValid.Should().BeFalse();
	}

	[Fact]
	public void Validate_IngredientWithNullAmountAndUnit_HasNoError()
	{
		var request = CreateValidRequest() with
		{
			Ingredients = [new SaveRecipeIngredient("Salt", null, null)],
		};

		var result = validator.TestValidate(request);

		result.ShouldNotHaveAnyValidationErrors();
	}

	[Fact]
	public void Validate_IngredientUnitExceedsMaxLength_HasError()
	{
		var request = CreateValidRequest() with
		{
			Ingredients = [new SaveRecipeIngredient("Flour", 500, new string('a', 51))],
		};

		var result = validator.TestValidate(request);

		result.IsValid.Should().BeFalse();
	}
}
