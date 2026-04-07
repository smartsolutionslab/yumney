using SmartSolutionsLab.Yumney.Recipes.Domain.Recipe;

namespace SmartSolutionsLab.Yumney.Recipes.Application.DTOs;

public sealed record ScrapedContent(string CleanedText, string SourceUrl);
