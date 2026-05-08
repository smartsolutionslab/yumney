using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Wolverine.EntityFrameworkCore;

namespace SmartSolutionsLab.Yumney.Shared.Events.Wolverine;

/// <summary>
/// Wolverine-aware variant of <c>AddYumneyNpgsqlDbContext</c>. Registers the
/// DbContext via <see cref="WolverineEntityCoreExtensions.AddDbContextWithWolverineIntegration{T}(IServiceCollection, Action{IServiceProvider, DbContextOptionsBuilder}, string)"/>
/// so an <see cref="IDbContextOutbox{T}"/> is available for typed-outbox publishing.
/// Use this instead of the plain <c>AddYumneyNpgsqlDbContext</c> in modules whose
/// event-store or integration-event publishers must be transactional.
/// </summary>
public static class YumneyOutboxDbContextRegistration
{
	public static IServiceCollection AddYumneyNpgsqlDbContextWithOutbox<TContext>(
		this IServiceCollection services,
		IConfiguration configuration,
		string connectionName,
		string migrationsHistoryTable,
		string wolverineSchema,
		params Type[] interceptorTypes)
		where TContext : DbContext
	{
		var connectionString = configuration.GetConnectionString(connectionName);

		services.AddDbContextWithWolverineIntegration<TContext>(
			(sp, options) =>
			{
				options.UseNpgsql(connectionString, npgsql => npgsql
					.MigrationsHistoryTable(migrationsHistoryTable)
					.EnableRetryOnFailure());

				foreach (var interceptorType in interceptorTypes)
				{
					options.AddInterceptors((IInterceptor)sp.GetRequiredService(interceptorType));
				}
			},
			wolverineSchema);

		return services;
	}
}
