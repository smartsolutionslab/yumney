using FluentAssertions;
using FluentValidation;
using SmartSolutionsLab.Yumney.Shared.CQRS;
using Xunit;

namespace SmartSolutionsLab.Yumney.Shared.Tests.CQRS;

public class FluentValidationExtensionsTests
{
	private readonly TestValidator validator = new();

	[Fact]
	public void MustBeValidHttpUrl_ValidHttpUrl_IsValid()
	{
		var result = validator.Validate(new TestRequest("http://example.com"));

		result.IsValid.Should().BeTrue();
	}

	[Fact]
	public void MustBeValidHttpUrl_ValidHttpsUrl_IsValid()
	{
		var result = validator.Validate(new TestRequest("https://example.com"));

		result.IsValid.Should().BeTrue();
	}

	[Fact]
	public void MustBeValidHttpUrl_FtpUrl_IsInvalid()
	{
		var result = validator.Validate(new TestRequest("ftp://example.com/file.txt"));

		result.IsValid.Should().BeFalse();
	}

	[Fact]
	public void MustBeValidHttpUrl_RelativeUrl_IsInvalid()
	{
		var result = validator.Validate(new TestRequest("/relative/path"));

		result.IsValid.Should().BeFalse();
	}

	[Fact]
	public void MustBeValidHttpUrl_RandomString_IsInvalid()
	{
		var result = validator.Validate(new TestRequest("not-a-url-at-all"));

		result.IsValid.Should().BeFalse();
	}

	[Fact]
	public void MustBeValidHttpUrl_ExceedsMaxLength_IsInvalid()
	{
		var longUrl = "https://example.com/" + new string('a', 2040);
		var result = validator.Validate(new TestRequest(longUrl));

		result.IsValid.Should().BeFalse();
	}

	[Fact]
	public void MustBeValidHttpUrl_AtMaxLength_IsValid()
	{
		var maxLengthValidator = new TestValidator(maxLength: 50);
		var url = "https://example.com/" + new string('a', 30);

		var result = maxLengthValidator.Validate(new TestRequest(url));

		result.IsValid.Should().BeTrue();
	}

	[Fact]
	public void MustBeValidHttpUrl_WithQueryParameters_IsValid()
	{
		var result = validator.Validate(new TestRequest("https://example.com/path?key=value&other=123"));

		result.IsValid.Should().BeTrue();
	}

	private sealed record TestRequest(string Url);

	private sealed class TestValidator : AbstractValidator<TestRequest>
	{
		public TestValidator(int maxLength = 2048)
		{
			RuleFor(x => x.Url).MustBeValidHttpUrl(maxLength);
		}
	}
}
