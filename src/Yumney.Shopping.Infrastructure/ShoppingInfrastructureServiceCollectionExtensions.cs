using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;
using SmartSolutionsLab.Yumney.Recipes.Client;
using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shared.Events;
using SmartSolutionsLab.Yumney.Shared.Events.Wolverine;
using SmartSolutionsLab.Yumney.Shared.Persistence;
using SmartSolutionsLab.Yumney.Shopping.Application.Interfaces;
using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingLedger;
using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingList;
using SmartSolutionsLab.Yumney.Shopping.Infrastructure.ExternalServices;
using SmartSolutionsLab.Yumney.Shopping.Infrastructure.Persistence;
using SmartSolutionsLab.Yumney.Shopping.Infrastructure.Persistence.EventStore;
using SmartSolutionsLab.Yumney.Shopping.Infrastructure.Persistence.ReadModel;
using SmartSolutionsLab.Yumney.Shopping.Infrastructure.Services;
using SmartSolutionsLab.Yumney.Users.Client;

namespace SmartSolutionsLab.Yumney.Shopping.Infrastructure;

public static class ShoppingInfrastructureServiceCollectionExtensions
{
	public static IServiceCollection AddShoppingInfrastructure(this IServiceCollection services, IConfiguration configuration)
	{
		services.AddYumneyNpgsqlDbContextWithOutbox<ShoppingDbContext>(
			configuration,
			"shoppingdb",
			"__ShoppingMigrationsHistory",
			"wolverine_shopping");
		services.AddYumneyNpgsqlDbContext<ShoppingReadDbContext>(
			configuration,
			"shoppingdb",
			"__ShoppingMigrationsHistory",
			typeof(QueryCountingInterceptor));

		services.AddScoped<IShoppingListEventStore, ShoppingListEventStore>();
		services.AddScoped<IShoppingEventStore, ShoppingEventStore>();
		services.AddScoped<IShoppingLedgerReadModelRepository, ShoppingLedgerReadModelRepository>();
		services.AddScoped<IShoppingListProjectionRepository, ShoppingListProjectionRepository>();
		services.AddScoped<IShoppingListProjectionRebuilder, ShoppingListProjectionRebuilder>();
		services.AddScoped<IIngredientBalanceReadModelRepository, IngredientBalanceReadModelRepository>();
		services.AddScoped<IStaplesProvider, HttpStaplesProvider>();
		services.AddScoped<IRecipeIngredientLookup, HttpRecipeIngredientLookup>();
		services.AddRecipesClient();
		services.AddScoped<IInboxStore, InboxStore<ShoppingDbContext>>();
		services.AddBusEventHandlersFromAssemblyContaining<ShoppingListProjection>();
		services.AddUsersClient();
		services.AddScoped<IShoppingUserDataPurger, ShoppingUserDataPurger>();
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
