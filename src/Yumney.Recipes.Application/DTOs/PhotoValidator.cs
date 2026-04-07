using SmartSolutionsLab.Yumney.Shared.Common;

namespace SmartSolutionsLab.Yumney.Recipes.Application.DTOs;

public static class PhotoValidator
{
    public const long MaxPhotoSizeBytes = 10 * 1024 * 1024;

    public static readonly HashSet<string> AllowedContentTypes =
    [
        MediaTypes.ImageJpeg,
        MediaTypes.ImagePng,
        MediaTypes.ImageWebp,
    ];

    public static Result Validate(PhotoData photo)
    {
        if (photo.Content.Length > MaxPhotoSizeBytes)
        {
            return Result.Failure(Commands.ImportRecipeErrors.PhotoTooLarge);
        }

        if (!AllowedContentTypes.Contains(photo.ContentType.ToLowerInvariant()))
        {
            return Result.Failure(Commands.ImportRecipeErrors.InvalidPhotoFormat);
        }

        return Result.Success();
    }

    public static Result ValidateMany(IReadOnlyList<PhotoData> photos, int maxPhotos)
    {
        if (photos.Count == 0 || photos.Count > maxPhotos)
        {
            return Result.Failure(Commands.ImportRecipeErrors.TooManyPhotos);
        }

        foreach (var photo in photos)
        {
            var result = Validate(photo);
            if (result.IsFailure) return result;
        }

        return Result.Success();
    }
}
