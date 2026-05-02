using FluentAssertions;
using SmartSolutionsLab.Yumney.Recipes.Api.Requests;
using SmartSolutionsLab.Yumney.Recipes.Api.Requests.Validator;
using Xunit;

namespace SmartSolutionsLab.Yumney.Recipes.Api.Tests.Requests;

public class ImportRecipeValidatorTests
{
	private readonly ImportRecipeValidator validator = new();

	[Fact]
	public void Validate_ValidHttpsUrl_IsValid()
	{
		var request = new ImportRecipe("https://example.com/recipe/123");

		var result = validator.Validate(request);

		result.IsValid.Should().BeTrue();
	}

	[Fact]
	public void Validate_ValidHttpUrl_IsValid()
	{
		var request = new ImportRecipe("http://example.com/recipe/123");

		var result = validator.Validate(request);

		result.IsValid.Should().BeTrue();
	}

	[Fact]
	public void Validate_UrlWithQueryParams_IsValid()
	{
		var request = new ImportRecipe("https://example.com/recipe?id=123&lang=en");

		var result = validator.Validate(request);

		result.IsValid.Should().BeTrue();
	}

	[Theory]
	[InlineData("")]
	[InlineData("   ")]
	public void Validate_EmptyUrl_IsNotValid(string url)
	{
		var request = new ImportRecipe(url);

		var result = validator.Validate(request);

		result.IsValid.Should().BeFalse();
		result.Errors.Should().Contain(e => e.PropertyName == "Url");
	}

	[Theory]
	[InlineData("not-a-url")]
	[InlineData("ftp://example.com/recipe")]
	[InlineData("example.com/recipe")]
	public void Validate_InvalidUrl_IsNotValid(string url)
	{
		var request = new ImportRecipe(url);

		var result = validator.Validate(request);

		result.IsValid.Should().BeFalse();
		result.Errors.Should().Contain(e => e.PropertyName == "Url");
	}

	[Fact]
	public void Validate_UrlExceedsMaxLength_IsNotValid()
	{
		var path = new string('a', 2040);
		var url = $"https://x.com/{path}";
		var request = new ImportRecipe(url);

		var result = validator.Validate(request);

		result.IsValid.Should().BeFalse();
		result.Errors.Should().Contain(e => e.PropertyName == "Url");
	}

	[Fact]
	public void Validate_UrlAtMaxLength_IsValid()
	{
		var prefix = "https://x.com/";
		var path = new string('a', 2048 - prefix.Length);
		var url = $"{prefix}{path}";
		var request = new ImportRecipe(url);

		var result = validator.Validate(request);

		result.Errors.Should().NotContain(
			e => e.PropertyName == "Url"
				&& e.ErrorCode == "MaximumLengthValidator");
	}

	[Fact]
	public void Validate_NullUrl_IsNotValid()
	{
		var request = new ImportRecipe(null!);

		var result = validator.Validate(request);

		result.IsValid.Should().BeFalse();
		result.Errors.Should().Contain(e => e.PropertyName == "Url");
	}

	[Fact]
	public void Validate_UrlWithFragment_IsValid()
	{
		var request = new ImportRecipe(
			"https://example.com/recipe#ingredients");

		var result = validator.Validate(request);

		result.IsValid.Should().BeTrue();
	}
}
