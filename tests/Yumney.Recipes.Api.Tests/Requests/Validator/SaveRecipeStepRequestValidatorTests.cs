using FluentAssertions;
using FluentValidation.TestHelper;
using SmartSolutionsLab.Yumney.Recipes.Api.Requests;
using SmartSolutionsLab.Yumney.Recipes.Api.Requests.Validator;
using SmartSolutionsLab.Yumney.Recipes.Domain.Recipe;
using Xunit;

namespace SmartSolutionsLab.Yumney.Recipes.Api.Tests.Requests.Validator;

public class SaveRecipeStepRequestValidatorTests
{
	private readonly SaveRecipeStepRequestValidator validator = new();

	[Fact]
	public void Validate_ValidRequest_HasNoErrors()
	{
		var request = new SaveRecipeStepRequest(1, "Cook pasta until al dente.");

		var result = validator.TestValidate(request);

		result.ShouldNotHaveAnyValidationErrors();
	}

	[Fact]
	public void Validate_ZeroNumber_HasError()
	{
		var request = new SaveRecipeStepRequest(0, "Cook pasta.");

		var result = validator.TestValidate(request);

		result.ShouldHaveValidationErrorFor(x => x.Number);
	}

	[Fact]
	public void Validate_NegativeNumber_HasError()
	{
		var request = new SaveRecipeStepRequest(-1, "Cook pasta.");

		var result = validator.TestValidate(request);

		result.ShouldHaveValidationErrorFor(x => x.Number);
	}

	[Fact]
	public void Validate_PositiveNumber_HasNoError()
	{
		var request = new SaveRecipeStepRequest(5, "Cook pasta.");

		var result = validator.TestValidate(request);

		result.ShouldNotHaveValidationErrorFor(x => x.Number);
	}

	[Theory]
	[InlineData(null)]
	[InlineData("")]
	[InlineData("   ")]
	public void Validate_EmptyDescription_HasError(string? description)
	{
		var request = new SaveRecipeStepRequest(1, description!);

		var result = validator.TestValidate(request);

		result.ShouldHaveValidationErrorFor(x => x.Description);
	}

	[Fact]
	public void Validate_DescriptionExceedsMaxLength_HasError()
	{
		var request = new SaveRecipeStepRequest(1, new string('a', StepDescription.MaxLength + 1));

		var result = validator.TestValidate(request);

		result.ShouldHaveValidationErrorFor(x => x.Description);
	}

	[Fact]
	public void Validate_DescriptionAtMaxLength_HasNoError()
	{
		var request = new SaveRecipeStepRequest(1, new string('a', StepDescription.MaxLength));

		var result = validator.TestValidate(request);

		result.ShouldNotHaveValidationErrorFor(x => x.Description);
	}
}
