using FluentValidation.TestHelper;
using Xunit;

namespace SmartSolutionsLab.Yumney.Recipes.Api.Tests.Requests.Validator;

public class ImportFromTextValidatorTests
{
	private readonly Api.Requests.Validator.ImportFromTextValidator validator = new();

	[Fact]
	public void Validate_ValidRequest_HasNoErrors()
	{
		var request = new Api.Requests.ImportFromText("Take 2 eggs and mix with flour...");

		var result = validator.TestValidate(request);

		result.ShouldNotHaveAnyValidationErrors();
	}

	[Theory]
	[InlineData(null)]
	[InlineData("")]
	[InlineData("   ")]
	public void Validate_EmptyText_HasError(string? text)
	{
		var request = new Api.Requests.ImportFromText(text!);

		var result = validator.TestValidate(request);

		result.ShouldHaveValidationErrorFor(request => request.Text);
	}
}
