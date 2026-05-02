using FluentValidation.TestHelper;
using Xunit;

namespace SmartSolutionsLab.Yumney.Recipes.Api.Tests.Requests.Validator;

public class ImportFromTextRequestValidatorTests
{
	private readonly Api.Requests.Validator.ImportFromTextRequestValidator validator = new();

	[Fact]
	public void Validate_ValidRequest_HasNoErrors()
	{
		var request = new Api.Requests.ImportFromTextRequestDto("Take 2 eggs and mix with flour...");

		var result = validator.TestValidate(request);

		result.ShouldNotHaveAnyValidationErrors();
	}

	[Theory]
	[InlineData(null)]
	[InlineData("")]
	[InlineData("   ")]
	public void Validate_EmptyText_HasError(string? text)
	{
		var request = new Api.Requests.ImportFromTextRequestDto(text!);

		var result = validator.TestValidate(request);

		result.ShouldHaveValidationErrorFor(x => x.Text);
	}
}
