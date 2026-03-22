using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.SemanticKernel;
using SmartSolutionsLab.Yumney.Recipes.Application.Interfaces;
using SmartSolutionsLab.Yumney.Recipes.Extraction.Services;

namespace SmartSolutionsLab.Yumney.Recipes.Extraction;

public static class ExtractionServiceCollectionExtensions
{
    public static IServiceCollection AddRecipeExtraction(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddHttpClient<IWebScraper, WebScraper>().AddStandardResilienceHandler();
        services.AddScoped<IRecipeExtractionService, SemanticKernelRecipeExtractionService>();

        var skOptions = configuration
            .GetSection(SemanticKernelOptions.SectionName)
            .Get<SemanticKernelOptions>() ?? new SemanticKernelOptions();

        var kernelBuilder = services.AddKernel();

        switch (skOptions.Provider)
        {
            case SemanticKernelOptions.ProviderAzureOpenAI:
                kernelBuilder.AddAzureOpenAIChatCompletion(skOptions.ModelId, skOptions.Endpoint, skOptions.ApiKey);
                break;
            case SemanticKernelOptions.ProviderOllama:
                var ollamaEndpoint = !string.IsNullOrEmpty(skOptions.Endpoint)
                    ? skOptions.Endpoint
                    : configuration.GetConnectionString("ollama")
                      ?? throw new InvalidOperationException("Ollama endpoint not configured. Provide SemanticKernel:Endpoint or ConnectionStrings:ollama.");
                kernelBuilder.AddOpenAIChatCompletion(skOptions.ModelId, new Uri(ollamaEndpoint), apiKey: null);
                break;
            default:
                kernelBuilder.AddOpenAIChatCompletion(skOptions.ModelId, skOptions.ApiKey);
                break;
        }

        return services;
    }
}
