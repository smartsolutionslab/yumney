using FluentValidation.TestHelper;
using SmartSolutionsLab.Yumney.Recipes.Api.Requests.Validator;
using SmartSolutionsLab.Yumney.Recipes.Application.DTOs;
using Xunit;

namespace SmartSolutionsLab.Yumney.Recipes.Api.Tests.Requests.Validator;

public class ParseIntentRequestValidatorTests
{
	private readonly ParseIntentRequestValidator validator = new();

	[Fact]
	public void Validate_ValidRequest_HasNoErrors()
	{
		var request = new ParseIntentRequestDto("I want to cook pasta", null);

		var result = validator.TestValidate(request);

		result.ShouldNotHaveAnyValidationErrors();
	}

	[Fact]
	public void Validate_ValidRequestWithContext_HasNoErrors()
	{
		var request = new ParseIntentRequestDto("Add more garlic", "recipe-editing");

		var result = validator.TestValidate(request);

		result.ShouldNotHaveAnyValidationErrors();
	}

	[Fact]
	public void Validate_NullContext_HasNoErrors()
	{
		var request = new ParseIntentRequestDto("Search for recipes", null);

		var result = validator.TestValidate(request);

		result.ShouldNotHaveValidationErrorFor(x => x.Context);
	}

	[Theory]
	[InlineData(null)]
	[InlineData("")]
	[InlineData("   ")]
	public void Validate_EmptyMessage_HasError(string? message)
	{
		var request = new ParseIntentRequestDto(message!, null);

		var result = validator.TestValidate(request);

		result.ShouldHaveValidationErrorFor(x => x.Message);
	}
}
