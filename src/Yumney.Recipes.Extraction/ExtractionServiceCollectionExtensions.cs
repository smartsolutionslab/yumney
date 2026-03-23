using System.Net;
using System.Net.Http.Headers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.SemanticKernel;
using SmartSolutionsLab.Yumney.Recipes.Application.Interfaces;
using SmartSolutionsLab.Yumney.Recipes.Extraction.Services;
using SmartSolutionsLab.Yumney.Shared.Common;

namespace SmartSolutionsLab.Yumney.Recipes.Extraction;

public static class ExtractionServiceCollectionExtensions
{
    public static IServiceCollection AddRecipeExtraction(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddHttpClient<IWebScraper, WebScraper>(client =>
            {
                client.DefaultRequestHeaders.UserAgent.ParseAdd(
                    "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/131.0.0.0 Safari/537.36");

                client.DefaultRequestHeaders.Accept.ParseAdd("text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8");
                client.DefaultRequestHeaders.AcceptLanguage.ParseAdd("en-US,en;q=0.9,de;q=0.8");
                client.DefaultRequestHeaders.AcceptEncoding.ParseAdd("gzip, deflate, br");

                client.DefaultRequestHeaders.Add("Sec-Fetch-Mode", "navigate");
                client.DefaultRequestHeaders.Add("Sec-Fetch-Site", "cross-site");
                client.DefaultRequestHeaders.Add("Sec-Fetch-Dest", "document");
                client.DefaultRequestHeaders.Add("Sec-Fetch-User", "?1");
                client.DefaultRequestHeaders.Add("Upgrade-Insecure-Requests", "1");
                client.DefaultRequestHeaders.Referrer = new Uri("https://www.google.com/");

                client.Timeout = TimeSpan.FromSeconds(30);
            })
            .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
            {
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Brotli | DecompressionMethods.Deflate,
                UseCookies = true,
                AllowAutoRedirect = true,
                MaxAutomaticRedirections = 5,
            })
            .AddStandardResilienceHandler();
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
                var ollamaEndpoint = skOptions.Endpoint.HasValue()
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
