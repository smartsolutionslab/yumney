using FluentAssertions;
using FluentValidation.TestHelper;
using SmartSolutionsLab.Yumney.Recipes.Api.Requests;
using SmartSolutionsLab.Yumney.Recipes.Api.Requests.Validator;
using Xunit;

namespace SmartSolutionsLab.Yumney.Recipes.Api.Tests.Requests;

#pragma warning disable SA1601
public partial class SaveRecipeValidatorTests
#pragma warning restore SA1601
{
	private readonly SaveRecipeValidator validator = new();

	[Fact]
	public void Validate_ValidRequest_HasNoErrors()
	{
		var request = CreateValidRequest();

		var result = validator.TestValidate(request);

		result.ShouldNotHaveAnyValidationErrors();
	}

	[Theory]
	[InlineData(null)]
	[InlineData("")]
	[InlineData("   ")]
	public void Validate_EmptyTitle_HasError(string? title)
	{
		var request = CreateValidRequest() with { Title = title! };

		var result = validator.TestValidate(request);

		result.ShouldHaveValidationErrorFor(x => x.Title);
	}

	[Fact]
	public void Validate_TitleExceedsMaxLength_HasError()
	{
		var request = CreateValidRequest() with { Title = new string('a', 201) };

		var result = validator.TestValidate(request);

		result.ShouldHaveValidationErrorFor(x => x.Title);
	}

	[Fact]
	public void Validate_TitleAtMaxLength_HasNoError()
	{
		var request = CreateValidRequest() with { Title = new string('a', 200) };

		var result = validator.TestValidate(request);

		result.ShouldNotHaveValidationErrorFor(x => x.Title);
	}

	[Fact]
	public void Validate_NullSourceUrl_HasNoError()
	{
		var request = CreateValidRequest() with { SourceUrl = null };

		var result = validator.TestValidate(request);

		result.ShouldNotHaveValidationErrorFor(x => x.SourceUrl);
	}

	[Fact]
	public void Validate_EmptySourceUrl_HasNoError()
	{
		var request = CreateValidRequest() with { SourceUrl = string.Empty };

		var result = validator.TestValidate(request);

		result.ShouldNotHaveValidationErrorFor(x => x.SourceUrl);
	}

	[Fact]
	public void Validate_ValidRequestWithNullSourceUrl_HasNoErrors()
	{
		var request = CreateValidRequest() with { SourceUrl = null };

		var result = validator.TestValidate(request);

		result.ShouldNotHaveAnyValidationErrors();
	}

	[Theory]
	[InlineData("not-a-url")]
	[InlineData("ftp://example.com")]
	public void Validate_InvalidSourceUrl_HasError(string sourceUrl)
	{
		var request = CreateValidRequest() with { SourceUrl = sourceUrl };

		var result = validator.TestValidate(request);

		result.ShouldHaveValidationErrorFor(x => x.SourceUrl);
	}

	[Fact]
	public void Validate_ValidHttpUrl_HasNoError()
	{
		var request = CreateValidRequest() with { SourceUrl = "http://example.com/recipe" };

		var result = validator.TestValidate(request);

		result.ShouldNotHaveValidationErrorFor(x => x.SourceUrl);
	}

	[Fact]
	public void Validate_ValidHttpsUrl_HasNoError()
	{
		var request = CreateValidRequest() with { SourceUrl = "https://example.com/recipe" };

		var result = validator.TestValidate(request);

		result.ShouldNotHaveValidationErrorFor(x => x.SourceUrl);
	}

	[Fact]
	public void Validate_EmptyIngredients_HasError()
	{
		var request = CreateValidRequest() with { Ingredients = [] };

		var result = validator.TestValidate(request);

		result.ShouldHaveValidationErrorFor(x => x.Ingredients);
	}

	[Fact]
	public void Validate_IngredientWithEmptyName_HasError()
	{
		var request = CreateValidRequest() with
		{
			Ingredients = [new SaveRecipeIngredient(string.Empty, null, null)],
		};

		var result = validator.TestValidate(request);

		result.IsValid.Should().BeFalse();
	}

	[Fact]
	public void Validate_EmptySteps_HasError()
	{
		var request = CreateValidRequest() with { Steps = [] };

		var result = validator.TestValidate(request);

		result.ShouldHaveValidationErrorFor(x => x.Steps);
	}

	[Fact]
	public void Validate_StepWithEmptyDescription_HasError()
	{
		var request = CreateValidRequest() with
		{
			Steps = [new SaveRecipeStep(1, string.Empty)],
		};

		var result = validator.TestValidate(request);

		result.IsValid.Should().BeFalse();
	}

	private static SaveRecipe CreateValidRequest()
	{
		return new SaveRecipe(
			"Pasta Carbonara",
			"A classic dish",
			[new SaveRecipeIngredient("Spaghetti", 400, "g")],
			[new SaveRecipeStep(1, "Cook pasta")],
			4,
			10,
			20,
			"medium",
			null,
			"https://example.com/recipe");
	}
}
