using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace SmartSolutionsLab.Yumney.Shared.Persistence;

public static class NpgsqlDbContextRegistration
{
	public static IServiceCollection AddYumneyNpgsqlDbContext<TContext>(
		this IServiceCollection services,
		IConfiguration configuration,
		string connectionName,
		string migrationsHistoryTable,
		params Type[] interceptorTypes)
		where TContext : DbContext
	{
		var connectionString = configuration.GetConnectionString(connectionName);

		services.AddDbContext<TContext>((serviceProvider, options) =>
		{
			options.UseNpgsql(connectionString, npgsql => npgsql
				.MigrationsHistoryTable(migrationsHistoryTable)
				.EnableRetryOnFailure());

			foreach (var interceptorType in interceptorTypes)
			{
				options.AddInterceptors((IInterceptor)serviceProvider.GetRequiredService(interceptorType));
			}
		});

		return services;
	}
}
