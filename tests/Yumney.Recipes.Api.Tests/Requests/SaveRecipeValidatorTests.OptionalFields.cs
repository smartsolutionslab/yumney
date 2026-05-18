using FluentValidation.TestHelper;
using Xunit;

namespace SmartSolutionsLab.Yumney.Recipes.Api.Tests.Requests;

#pragma warning disable SA1601
public partial class SaveRecipeValidatorTests
#pragma warning restore SA1601
{
	[Fact]
	public void Validate_NullOptionalFields_HasNoErrors()
	{
		var request = CreateValidRequest() with
		{
			Description = null,
			Servings = null,
			PrepTimeMinutes = null,
			CookTimeMinutes = null,
			Difficulty = null,
			ImageUrl = null,
		};

		var result = validator.TestValidate(request);

		result.ShouldNotHaveAnyValidationErrors();
	}

	[Fact]
	public void Validate_ZeroServings_HasError()
	{
		var request = CreateValidRequest() with { Servings = 0 };

		var result = validator.TestValidate(request);

		result.ShouldHaveValidationErrorFor(x => x.Servings);
	}

	[Fact]
	public void Validate_NegativeServings_HasError()
	{
		var request = CreateValidRequest() with { Servings = -1 };

		var result = validator.TestValidate(request);

		result.ShouldHaveValidationErrorFor(x => x.Servings);
	}

	[Fact]
	public void Validate_PositiveServings_HasNoError()
	{
		var request = CreateValidRequest() with { Servings = 1 };

		var result = validator.TestValidate(request);

		result.ShouldNotHaveValidationErrorFor(x => x.Servings);
	}

	[Fact]
	public void Validate_NegativePrepTime_HasError()
	{
		var request = CreateValidRequest() with { PrepTimeMinutes = -1 };

		var result = validator.TestValidate(request);

		result.ShouldHaveValidationErrorFor(x => x.PrepTimeMinutes);
	}

	[Fact]
	public void Validate_ZeroPrepTime_HasNoError()
	{
		var request = CreateValidRequest() with { PrepTimeMinutes = 0 };

		var result = validator.TestValidate(request);

		result.ShouldNotHaveValidationErrorFor(x => x.PrepTimeMinutes);
	}

	[Fact]
	public void Validate_NegativeCookTime_HasError()
	{
		var request = CreateValidRequest() with { CookTimeMinutes = -1 };

		var result = validator.TestValidate(request);

		result.ShouldHaveValidationErrorFor(x => x.CookTimeMinutes);
	}

	[Fact]
	public void Validate_ZeroCookTime_HasNoError()
	{
		var request = CreateValidRequest() with { CookTimeMinutes = 0 };

		var result = validator.TestValidate(request);

		result.ShouldNotHaveValidationErrorFor(x => x.CookTimeMinutes);
	}
}
