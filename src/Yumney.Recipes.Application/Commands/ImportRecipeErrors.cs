namespace Yumney.Recipes.Application.Commands;

public static class ImportRecipeErrors
{
    public const string PageUnreachable = "IMPORT_PAGE_UNREACHABLE";
    public const string ScrapeTimeout = "IMPORT_SCRAPE_TIMEOUT";
    public const string NoRecipeFound = "IMPORT_NO_RECIPE_FOUND";
    public const string ExtractionFailed = "IMPORT_EXTRACTION_FAILED";
}
