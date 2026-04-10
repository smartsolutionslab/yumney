using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;
using SmartSolutionsLab.Yumney.Recipes.Application.Interfaces;
using SmartSolutionsLab.Yumney.Recipes.Extraction.Services;
using SmartSolutionsLab.Yumney.Shared.Common;

namespace SmartSolutionsLab.Yumney.Recipes.Extraction;

public static class ExtractionServiceCollectionExtensions
{
    public static IServiceCollection AddRecipeExtraction(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddHttpClient<IWebScraper, WebScraper>(BrowserHttpClientDefaults.ConfigureHttpClient)
            .ConfigurePrimaryHttpMessageHandler(BrowserHttpClientDefaults.CreateHandler)
            .AddStandardResilienceHandler();
        services.AddScoped<IRecipeExtractionService, SemanticKernelRecipeExtractionService>();
        services.AddScoped<IIngredientRecognitionService, SemanticKernelIngredientRecognitionService>();
        services.AddScoped<IChatService, SemanticKernelChatService>();
        services.AddScoped<IIntentParserService, SemanticKernelIntentParserService>();

        var skOptions = configuration
            .GetSection(SemanticKernelOptions.SectionName)
            .Get<SemanticKernelOptions>() ?? new SemanticKernelOptions();

        var kernelBuilder = services.AddKernel();

        var (provider, modelId, endpoint, apiKey) = skOptions;

        switch (provider)
        {
            case SemanticKernelOptions.ProviderAzureOpenAI:
                kernelBuilder.AddAzureOpenAIChatCompletion(modelId, endpoint, apiKey);
                break;
            case SemanticKernelOptions.ProviderOllama:
                var ollamaEndpoint = GetOllamaEndpoint(configuration, skOptions);
                kernelBuilder.AddOpenAIChatCompletion(modelId, new Uri(ollamaEndpoint), apiKey: null);
                break;
            default:
                kernelBuilder.AddOpenAIChatCompletion(modelId, apiKey);
                break;
        }

        return services;
    }

    private static string GetOllamaEndpoint(IConfiguration configuration, SemanticKernelOptions skOptions)
    {
        return skOptions.Endpoint.HasValue()
            ? skOptions.Endpoint
            : configuration.GetConnectionString("ollama")
              ?? throw new InvalidOperationException("Ollama endpoint not configured. Provide SemanticKernel:Endpoint or ConnectionStrings:ollama.");
    }
}
