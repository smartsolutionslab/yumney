using FluentAssertions;
using SmartSolutionsLab.Yumney.Recipes.Application.DTOs;
using Xunit;

namespace SmartSolutionsLab.Yumney.Recipes.Application.Tests.Commands;

public class PhotoDataValidatorTests
{
	private readonly PhotoDataValidator validator = new();

	[Theory]
	[InlineData("image/jpeg")]
	[InlineData("image/png")]
	[InlineData("image/webp")]
	public void Validate_AllowedContentType_IsValid(string contentType)
	{
		var photo = new PhotoData(new byte[100], contentType, "photo.jpg");

		var result = validator.Validate(photo);

		result.IsValid.Should().BeTrue();
	}

	[Theory]
	[InlineData("application/pdf")]
	[InlineData("image/gif")]
	[InlineData("image/bmp")]
	[InlineData("text/plain")]
	public void Validate_DisallowedContentType_IsInvalid(string contentType)
	{
		var photo = new PhotoData(new byte[100], contentType, "file.pdf");

		var result = validator.Validate(photo);

		result.IsValid.Should().BeFalse();
		result.Errors.Should().Contain(e => e.PropertyName == "ContentType");
	}

	[Fact]
	public void Validate_EmptyContent_IsInvalid()
	{
		var photo = new PhotoData([], "image/jpeg", "empty.jpg");

		var result = validator.Validate(photo);

		result.IsValid.Should().BeFalse();
		result.Errors.Should().Contain(e => e.PropertyName == "Content");
	}

	[Fact]
	public void Validate_OversizedContent_IsInvalid()
	{
		var photo = new PhotoData(new byte[PhotoDataValidator.MaxPhotoSizeBytes + 1], "image/jpeg", "large.jpg");

		var result = validator.Validate(photo);

		result.IsValid.Should().BeFalse();
		result.Errors.Should().Contain(e => e.PropertyName == "Content");
	}

	[Fact]
	public void Validate_ExactMaxSize_IsValid()
	{
		var photo = new PhotoData(new byte[PhotoDataValidator.MaxPhotoSizeBytes], "image/jpeg", "max.jpg");

		var result = validator.Validate(photo);

		result.IsValid.Should().BeTrue();
	}

	[Fact]
	public void Validate_EmptyFileName_IsInvalid()
	{
		var photo = new PhotoData(new byte[100], "image/jpeg", string.Empty);

		var result = validator.Validate(photo);

		result.IsValid.Should().BeFalse();
		result.Errors.Should().Contain(e => e.PropertyName == "FileName");
	}

	[Fact]
	public void Validate_EmptyContentType_IsInvalid()
	{
		var photo = new PhotoData(new byte[100], string.Empty, "photo.jpg");

		var result = validator.Validate(photo);

		result.IsValid.Should().BeFalse();
		result.Errors.Should().Contain(e => e.PropertyName == "ContentType");
	}

	[Fact]
	public void Validate_UppercaseContentType_IsValid()
	{
		var photo = new PhotoData(new byte[100], "IMAGE/JPEG", "photo.jpg");

		var result = validator.Validate(photo);

		result.IsValid.Should().BeTrue();
	}
}
