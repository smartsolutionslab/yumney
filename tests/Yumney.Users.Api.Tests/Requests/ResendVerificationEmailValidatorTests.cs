using FluentAssertions;
using Xunit;

namespace SmartSolutionsLab.Yumney.Users.Api.Tests.Requests;

public class ResendVerificationEmailValidatorTests
{
	private readonly Api.Requests.ResendVerificationEmailValidator validator = new();

	[Fact]
	public void Validate_ValidRequest_IsValid()
	{
		var request = new Api.Requests.ResendVerificationEmail("test@example.com");

		var result = validator.Validate(request);

		result.IsValid.Should().BeTrue();
	}

	[Theory]
	[InlineData("")]
	[InlineData("   ")]
	[InlineData("not-an-email")]
	public void Validate_InvalidEmail_IsNotValid(string email)
	{
		var request = new Api.Requests.ResendVerificationEmail(email);

		var result = validator.Validate(request);

		result.IsValid.Should().BeFalse();
		result.Errors.Should().Contain(e => e.PropertyName == "Email");
	}

	[Fact]
	public void Validate_EmailAtMaxLength_IsValid()
	{
		var localPart = new string('a', 242);
		var email = $"{localPart}@example.com";
		var request = new Api.Requests.ResendVerificationEmail(email);

		var result = validator.Validate(request);

		result.Errors.Should().NotContain(e => e.PropertyName == "Email" && e.ErrorCode == "MaximumLengthValidator");
	}

	[Fact]
	public void Validate_EmailExceedsMaxLength_IsNotValid()
	{
		var localPart = new string('a', 243);
		var email = $"{localPart}@example.com";
		var request = new Api.Requests.ResendVerificationEmail(email);

		var result = validator.Validate(request);

		result.IsValid.Should().BeFalse();
		result.Errors.Should().Contain(e => e.PropertyName == "Email");
	}
}
