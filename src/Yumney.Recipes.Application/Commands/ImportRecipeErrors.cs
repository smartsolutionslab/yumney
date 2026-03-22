using SmartSolutionsLab.Yumney.Shared.Common;

namespace SmartSolutionsLab.Yumney.Recipes.Application.Commands;

public static class ImportRecipeErrors
{
    public static readonly ApiError PageUnreachable = new("IMPORT_PAGE_UNREACHABLE", "Could not reach the website.", 502);
    public static readonly ApiError ScrapeTimeout = new("IMPORT_SCRAPE_TIMEOUT", "Extraction timed out.", 504);
    public static readonly ApiError NoRecipeFound = new("IMPORT_NO_RECIPE_FOUND", "No recipe found on this page.", 404);
    public static readonly ApiError ExtractionFailed = new("IMPORT_EXTRACTION_FAILED", "Recipe extraction failed.", 500);
    public static readonly ApiError ContentTooLarge = new("IMPORT_CONTENT_TOO_LARGE", "The page content is too large to process.", 413);
    public static readonly ApiError TooManyPhotos = new("IMPORT_TOO_MANY_PHOTOS", "Too many photos. Maximum 10 images allowed.", 400);
    public static readonly ApiError PhotoTooLarge = new("IMPORT_PHOTO_TOO_LARGE", "Photo exceeds the maximum size of 10 MB.", 413);
    public static readonly ApiError InvalidPhotoFormat = new("IMPORT_INVALID_PHOTO_FORMAT", "Unsupported file format. Use JPG, PNG, or WebP.", 400);
}
