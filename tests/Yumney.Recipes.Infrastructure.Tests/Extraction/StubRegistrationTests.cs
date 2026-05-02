using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SmartSolutionsLab.Yumney.Recipes.Application.Interfaces;
using SmartSolutionsLab.Yumney.Recipes.Extraction;
using SmartSolutionsLab.Yumney.Recipes.Extraction.TestStubs;
using Xunit;

namespace SmartSolutionsLab.Yumney.Recipes.Infrastructure.Tests.Extraction;

public class StubRegistrationTests
{
	[Fact]
	public void AddRecipeExtraction_WithE2ETestsTrue_ResolvesStubImplementations()
	{
		var services = BuildServiceCollection(e2eTests: true);
		using var provider = services.BuildServiceProvider();

		provider.GetRequiredService<IWebScraper>().Should().BeOfType<StubWebScraper>();
		provider.GetRequiredService<IRecipeExtractionService>().Should().BeOfType<StubRecipeExtractionService>();
		provider.GetRequiredService<IRecipeSuggestionService>().Should().BeOfType<StubRecipeSuggestionService>();
		provider.GetRequiredService<IIngredientRecognitionService>().Should().BeOfType<StubIngredientRecognitionService>();
		provider.GetRequiredService<IChatService>().Should().BeOfType<StubChatService>();
		provider.GetRequiredService<IIntentParserService>().Should().BeOfType<StubIntentParserService>();
		provider.GetRequiredService<IIngredientCategoryService>().Should().BeOfType<StubIngredientCategoryService>();
	}

	[Fact]
	public void AddRecipeExtraction_WithE2ETestsFalse_DoesNotResolveStubs()
	{
		var services = BuildServiceCollection(e2eTests: false);
		using var provider = services.BuildServiceProvider();

		provider.GetRequiredService<IWebScraper>().Should().NotBeOfType<StubWebScraper>();
		provider.GetRequiredService<IRecipeExtractionService>().Should().NotBeOfType<StubRecipeExtractionService>();
	}

	private static ServiceCollection BuildServiceCollection(bool e2eTests)
	{
		var configuration = new ConfigurationBuilder()
			.AddInMemoryCollection(new Dictionary<string, string?> { ["E2ETests"] = e2eTests ? "true" : "false" })
			.Build();

		var services = new ServiceCollection();
		services.AddRecipeExtraction(configuration);
		return services;
	}
}
