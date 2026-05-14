using FluentValidation;
using SmartSolutionsLab.Yumney.Shared.Common;

namespace SmartSolutionsLab.Yumney.Recipes.Application.DTOs;

public sealed class PhotoDataValidator : AbstractValidator<PhotoData>
{
	public const long MaxPhotoSizeBytes = 10 * 1024 * 1024;
	public const int MaxPhotos = 10;

	public PhotoDataValidator()
	{
		RuleFor(photo => photo.Content)
			.NotEmpty()
			.WithMessage("Photo content must not be empty.")
			.Must(content => content.Length <= MaxPhotoSizeBytes)
			.WithMessage($"Photo exceeds the maximum size of {MaxPhotoSizeBytes / (1024 * 1024)} MB.");

		RuleFor(photo => photo.ContentType)
			.NotEmpty()
			.Must(contentType => AllowedContentTypes.Contains(contentType.ToLowerInvariant()))
			.WithMessage("Unsupported file format. Use JPG, PNG, or WebP.");

		RuleFor(photo => photo.FileName)
			.NotEmpty();
	}

	internal static readonly HashSet<string> AllowedContentTypes =
	[
		MediaTypes.ImageJpeg,
		MediaTypes.ImagePng,
		MediaTypes.ImageWebp
	];
}
