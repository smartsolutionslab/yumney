using FluentValidation.TestHelper;
using SmartSolutionsLab.Yumney.Recipes.Api.Requests.Validator;
using SmartSolutionsLab.Yumney.Recipes.Application.DTOs;
using Xunit;

namespace SmartSolutionsLab.Yumney.Recipes.Api.Tests.Requests.Validator;

public class ChatRequestValidatorTests
{
	private readonly ChatRequestValidator validator = new();

	[Fact]
	public void Validate_ValidRequest_HasNoErrors()
	{
		var request = new ChatRequestDto("How do I cook pasta?", []);

		var result = validator.TestValidate(request);

		result.ShouldNotHaveAnyValidationErrors();
	}

	[Fact]
	public void Validate_ValidRequestWithHistory_HasNoErrors()
	{
		var history = new List<ChatMessageDto>
		{
			new("user", "Hello"),
			new("assistant", "Hi there!"),
		};
		var request = new ChatRequestDto("Follow-up question", history);

		var result = validator.TestValidate(request);

		result.ShouldNotHaveAnyValidationErrors();
	}

	[Theory]
	[InlineData(null)]
	[InlineData("")]
	[InlineData("   ")]
	public void Validate_EmptyMessage_HasError(string? message)
	{
		var request = new ChatRequestDto(message!, []);

		var result = validator.TestValidate(request);

		result.ShouldHaveValidationErrorFor(x => x.Message);
	}
}
