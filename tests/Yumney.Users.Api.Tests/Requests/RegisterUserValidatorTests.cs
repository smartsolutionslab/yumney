using FluentAssertions;
using SmartSolutionsLab.Yumney.Users.Api.Requests;
using Xunit;

namespace SmartSolutionsLab.Yumney.Users.Api.Tests.Requests;

public class RegisterUserValidatorTests
{
	private readonly RegisterUserValidator validator = new();

	[Fact]
	public void Validate_ValidRequest_IsValid()
	{
		RegisterUser request = new RegisterUser("test@example.com", "Password1", "Test User");

		var result = validator.Validate(request);

		result.IsValid.Should().BeTrue();
	}

	[Theory]
	[InlineData("")]
	[InlineData("   ")]
	[InlineData("not-an-email")]
	public void Validate_InvalidEmail_IsNotValid(string email)
	{
		var request = new RegisterUser(email, "Password1", "Test User");

		var result = validator.Validate(request);

		result.IsValid.Should().BeFalse();
		result.Errors.Should().Contain(e => e.PropertyName == "Email");
	}

	[Theory]
	[InlineData("short1A")]
	[InlineData("nouppercase1")]
	[InlineData("NOLOWERCASE1")]
	[InlineData("NoDigitsHere")]
	public void Validate_WeakPassword_IsNotValid(string password)
	{
		var request = new RegisterUser("test@example.com", password, "Test User");

		var result = validator.Validate(request);

		result.IsValid.Should().BeFalse();
		result.Errors.Should().Contain(e => e.PropertyName == "Password");
	}

	[Theory]
	[InlineData("")]
	[InlineData("   ")]
	public void Validate_EmptyDisplayName_IsNotValid(string displayName)
	{
		var request = new RegisterUser("test@example.com", "Password1", displayName);

		var result = validator.Validate(request);

		result.IsValid.Should().BeFalse();
		result.Errors.Should().Contain(e => e.PropertyName == "DisplayName");
	}

	[Theory]
	[InlineData("user+test@example.com")]
	[InlineData("user.name@example.com")]
	[InlineData("user_name@example.co.uk")]
	public void Validate_EmailWithSpecialChars_IsValid(string email)
	{
		var request = new RegisterUser(email, "Password1", "Test User");

		var result = validator.Validate(request);

		result.IsValid.Should().BeTrue();
	}

	[Fact]
	public void Validate_EmailAtMaxLength_IsValid()
	{
		var localPart = new string('a', 242);
		var email = $"{localPart}@example.com";

		var request = new RegisterUser(email, "Password1", "Test User");

		var result = validator.Validate(request);

		result.Errors.Should().NotContain(e => e.PropertyName == "Email" && e.ErrorCode == "MaximumLengthValidator");
	}

	[Fact]
	public void Validate_EmailExceedsMaxLength_IsNotValid()
	{
		var localPart = new string('a', 243);
		var email = $"{localPart}@example.com";

		var request = new RegisterUser(email, "Password1", "Test User");

		var result = validator.Validate(request);

		result.IsValid.Should().BeFalse();
		result.Errors.Should().Contain(e => e.PropertyName == "Email");
	}

	[Fact]
	public void Validate_DisplayNameAtMaxLength_IsValid()
	{
		var displayName = new string('A', 200);
		var request = new RegisterUser("test@example.com", "Password1", displayName);

		var result = validator.Validate(request);

		result.IsValid.Should().BeTrue();
	}

	[Fact]
	public void Validate_DisplayNameExceedsMaxLength_IsNotValid()
	{
		var displayName = new string('A', 201);
		var request = new RegisterUser("test@example.com", "Password1", displayName);

		var result = validator.Validate(request);

		result.IsValid.Should().BeFalse();
		result.Errors.Should().Contain(e => e.PropertyName == "DisplayName");
	}

	[Fact]
	public void Validate_PasswordAtMinLength_IsValid()
	{
		var request = new RegisterUser("test@example.com", "Abcdef1x", "Test User");

		var result = validator.Validate(request);

		result.IsValid.Should().BeTrue();
	}
}
