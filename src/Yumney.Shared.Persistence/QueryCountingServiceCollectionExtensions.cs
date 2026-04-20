using Microsoft.Extensions.DependencyInjection;

namespace SmartSolutionsLab.Yumney.Shared.Persistence;

public static class QueryCountingServiceCollectionExtensions
{
	/// <summary>
	/// Registers a scoped <see cref="IQueryCounter"/> and
	/// <see cref="QueryCountingInterceptor"/>. DbContexts opt in by
	/// resolving the interceptor from the service provider:
	/// <code>
	/// options.AddInterceptors(sp.GetRequiredService&lt;QueryCountingInterceptor&gt;());
	/// </code>
	/// </summary>
	/// <param name="services">The service collection to register into.</param>
	/// <returns>The service collection, for chaining.</returns>
	public static IServiceCollection AddQueryCounting(this IServiceCollection services)
	{
		services.AddScoped<IQueryCounter, QueryCounter>();
		services.AddScoped<QueryCountingInterceptor>();
		return services;
	}
}
