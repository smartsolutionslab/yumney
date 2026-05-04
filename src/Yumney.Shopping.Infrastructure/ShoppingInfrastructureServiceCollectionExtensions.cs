using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;
using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shared.Events;
using SmartSolutionsLab.Yumney.Shared.Persistence;
using SmartSolutionsLab.Yumney.Shared.Web;
using SmartSolutionsLab.Yumney.Shopping.Application.Interfaces;
using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingLedger;
using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingList;
using SmartSolutionsLab.Yumney.Shopping.Infrastructure.ExternalServices;
using SmartSolutionsLab.Yumney.Shopping.Infrastructure.Persistence;
using SmartSolutionsLab.Yumney.Shopping.Infrastructure.Persistence.EventStore;
using SmartSolutionsLab.Yumney.Shopping.Infrastructure.Persistence.ReadModel;
using SmartSolutionsLab.Yumney.Shopping.Infrastructure.Services;

namespace SmartSolutionsLab.Yumney.Shopping.Infrastructure;

public static class ShoppingInfrastructureServiceCollectionExtensions
{
	public static IServiceCollection AddShoppingInfrastructure(this IServiceCollection services, IConfiguration configuration)
	{
		services.AddYumneyNpgsqlDbContext<ShoppingDbContext>(configuration, "shoppingdb", "__ShoppingMigrationsHistory");
		services.AddYumneyNpgsqlDbContext<ShoppingReadDbContext>(
			configuration,
			"shoppingdb",
			"__ShoppingMigrationsHistory",
			typeof(QueryCountingInterceptor));

		services.AddScoped<IShoppingListEventStore, EfCoreShoppingListEventStore>();
		services.AddScoped<IShoppingEventStore, EfCoreShoppingEventStore>();
		services.AddScoped<IShoppingLedgerReadModelRepository, ShoppingLedgerReadModelRepository>();
		services.AddScoped<IShoppingListProjectionRepository, EfCoreShoppingListProjectionRepository>();
		services.AddScoped<IShoppingListProjectionRebuilder, ShoppingListProjectionRebuilder>();
		services.AddScoped<IIngredientBalanceReadModelRepository, IngredientBalanceReadModelRepository>();
		services.AddScoped<IStaplesProvider, HttpStaplesProvider>();
		services.AddScoped<IRecipeIngredientLookup, HttpRecipeIngredientLookup>();
		services.AddYumneyServiceClient("recipes-api");
		services.AddScoped<IInboxStore, EfCoreInboxStore<ShoppingDbContext>>();
		services.AddBusEventHandlersFromAssemblyContaining<ShoppingListProjection>();
		services.AddYumneyServiceClient("users-api");
		services.AddHealthChecks().AddDbContextCheck<ShoppingDbContext>("shoppingdb");

		AddShoppingItemCategorizer(services, configuration);

		return services;
	}

	private static void AddShoppingItemCategorizer(IServiceCollection services, IConfiguration configuration)
	{
		if (configuration.GetValue<bool>("E2ETests"))
		{
			services.AddSingleton<IShoppingItemCategorizer, StubShoppingItemCategorizer>();
			return;
		}

		services.AddScoped<IShoppingItemCategorizer, SemanticKernelShoppingItemCategorizer>();

		var skOptions = configuration.GetSection(SemanticKernelOptions.SectionName).Get<SemanticKernelOptions>()
			?? new SemanticKernelOptions();
		var (provider, modelId, endpoint, apiKey) = skOptions;

		var kernelBuilder = services.AddKernel();
		switch (provider)
		{
			case SemanticKernelOptions.ProviderAzureOpenAI:
				kernelBuilder.AddAzureOpenAIChatCompletion(modelId, endpoint, apiKey);
				break;
			case SemanticKernelOptions.ProviderOllama:
				var ollama = endpoint.HasValue() ? endpoint : configuration.GetConnectionString("ollama");
				if (ollama is not null)
				{
					kernelBuilder.AddOpenAIChatCompletion(modelId, new Uri(ollama), apiKey: null);
				}

				break;
			default:
				if (apiKey.HasValue())
				{
					kernelBuilder.AddOpenAIChatCompletion(modelId, apiKey);
				}

				break;
		}
	}
}
