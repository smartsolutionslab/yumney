using SmartSolutionsLab.Yumney.Shared.Common;

namespace SmartSolutionsLab.Yumney.Recipes.Application.Commands;

public static class ImportRecipeErrors
{
    public static readonly ApiError PageUnreachable = new("IMPORT_PAGE_UNREACHABLE", "Could not reach the website.", 502);
    public static readonly ApiError ScrapeTimeout = new("IMPORT_SCRAPE_TIMEOUT", "Extraction timed out.", 504);
    public static readonly ApiError NoRecipeFound = new("IMPORT_NO_RECIPE_FOUND", "No recipe found on this page.", 404);
    public static readonly ApiError ExtractionFailed = new("IMPORT_EXTRACTION_FAILED", "Recipe extraction failed.", 500);
}
