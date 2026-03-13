namespace SmartSolutionsLab.Yumney.Recipes.Infrastructure.Services;

public sealed class ScrapingOptions
{
    public const string SectionName = "Scraping";

    public int MaxContentLength { get; init; } = 12_000;

    public int MaxRawHtmlLength { get; init; } = 500_000;
}
