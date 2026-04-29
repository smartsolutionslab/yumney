using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using SmartSolutionsLab.Yumney.MealPlan.Infrastructure.Persistence;
using SmartSolutionsLab.Yumney.Recipes.Infrastructure.Persistence;
using SmartSolutionsLab.Yumney.Shopping.Application.Interfaces;
using SmartSolutionsLab.Yumney.Shopping.Infrastructure.Persistence;
using SmartSolutionsLab.Yumney.Users.Infrastructure.Persistence;

namespace SmartSolutionsLab.Yumney.MigrationRunner;

/// <summary>
/// Background worker that applies EF Core migrations for all modules on startup.
/// Uses PostgreSQL advisory locks to prevent concurrent migration from multiple instances.
///
/// Dashboard-driven modes (set via configuration before start):
/// <list type="bullet">
///   <item><c>Persistence:ResetMealPlanOnly=true</c> — drops and re-migrates the
///   MealPlan database only. Wipes the event-sourced MealPlan store.</item>
///   <item><c>Persistence:ResetShoppingOnly=true</c> — drops and re-migrates the
///   Shopping database only. Wipes events, metadata, projections, and legacy
///   lists in one shot.</item>
///   <item><c>Persistence:RebuildShoppingProjections=true</c> — truncates the
///   ShoppingList projection tables and replays the event store into them.
///   Other Shopping data (events, metadata, legacy lists) is untouched.</item>
///   <item><c>Persistence:BackfillShoppingEvents=true</c> — synthesises events
///   for legacy <c>ShoppingLists</c> rows that don't yet have an event-store
///   entry. One-off; idempotent for already-backfilled lists.</item>
/// </list>
/// </summary>
public sealed partial class MigrationWorker(
	IServiceProvider serviceProvider,
	IHostApplicationLifetime lifetime,
	IConfiguration configuration,
	ILogger<MigrationWorker> logger) : BackgroundService
{
	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		try
		{
			if (configuration.GetValue<bool>("Persistence:BackfillShoppingEvents"))
			{
				await BackfillShoppingEventsAsync(stoppingToken);
			}
			else if (configuration.GetValue<bool>("Persistence:RebuildShoppingProjections"))
			{
				await RebuildShoppingProjectionsAsync(stoppingToken);
			}
			else if (configuration.GetValue<bool>("Persistence:ResetMealPlanOnly"))
			{
				await ApplyMigrationsAsync<MealPlanDbContext>("MealPlan", stoppingToken, reset: true);
			}
			else if (configuration.GetValue<bool>("Persistence:ResetShoppingOnly"))
			{
				await ApplyMigrationsAsync<ShoppingDbContext>("Shopping", stoppingToken, reset: true);
			}
			else
			{
				await ApplyMigrationsAsync<RecipesDbContext>("Recipes", stoppingToken);
				await ApplyMigrationsAsync<UsersDbContext>("Users", stoppingToken);
				await ApplyMigrationsAsync<ShoppingDbContext>("Shopping", stoppingToken);
				await ApplyMigrationsAsync<MealPlanDbContext>("MealPlan", stoppingToken);
			}

			LogAllMigrationsApplied(logger);
		}
		catch (Exception ex)
		{
			LogMigrationFailed(logger, ex);
			Environment.ExitCode = 1;
		}

		lifetime.StopApplication();
	}

	[LoggerMessage(Level = LogLevel.Information, Message = "All migrations applied successfully")]
	private static partial void LogAllMigrationsApplied(ILogger logger);

	[LoggerMessage(Level = LogLevel.Critical, Message = "Migration failed")]
	private static partial void LogMigrationFailed(ILogger logger, Exception ex);

	[LoggerMessage(Level = LogLevel.Information, Message = "{Module}: no pending migrations")]
	private static partial void LogNoPendingMigrations(ILogger logger, string module);

	[LoggerMessage(Level = LogLevel.Information, Message = "{Module}: applying {Count} pending migration(s): {Migrations}")]
	private static partial void LogApplyingMigrations(ILogger logger, string module, int count, string migrations);

	[LoggerMessage(Level = LogLevel.Information, Message = "{Module}: {Count} migration(s) now applied")]
	private static partial void LogMigrationsApplied(ILogger logger, string module, int count);

	[LoggerMessage(Level = LogLevel.Information, Message = "{Module}: acquiring advisory lock {LockId}")]
	private static partial void LogAcquiringLock(ILogger logger, string module, int lockId);

	[LoggerMessage(Level = LogLevel.Information, Message = "{Module}: advisory lock released")]
	private static partial void LogLockReleased(ILogger logger, string module);

	[LoggerMessage(Level = LogLevel.Warning, Message = "{Module}: dropping database before migrating")]
	private static partial void LogResettingDatabase(ILogger logger, string module);

	[LoggerMessage(Level = LogLevel.Information, Message = "Shopping: rebuilt {Count} projection events")]
	private static partial void LogShoppingProjectionsRebuilt(ILogger logger, int count);

	[LoggerMessage(Level = LogLevel.Information, Message = "Shopping: backfilled events for {Count} legacy list(s)")]
	private static partial void LogShoppingEventsBackfilled(ILogger logger, int count);

	private static int GenerateLockId(string moduleName)
	{
		var hash = SHA256.HashData(Encoding.UTF8.GetBytes($"yumney-migration-{moduleName}"));
		return BitConverter.ToInt32(hash, 0);
	}

	private async Task RebuildShoppingProjectionsAsync(CancellationToken cancellationToken)
	{
		await using var scope = serviceProvider.CreateAsyncScope();
		var rebuilder = scope.ServiceProvider.GetRequiredService<IShoppingListProjectionRebuilder>();
		var replayed = await rebuilder.RebuildAsync(cancellationToken);
		LogShoppingProjectionsRebuilt(logger, replayed);
	}

	private async Task BackfillShoppingEventsAsync(CancellationToken cancellationToken)
	{
		await using var scope = serviceProvider.CreateAsyncScope();
		var backfill = scope.ServiceProvider.GetRequiredService<IShoppingListBackfillService>();
		var count = await backfill.BackfillAsync(cancellationToken);
		LogShoppingEventsBackfilled(logger, count);
	}

	private async Task ApplyMigrationsAsync<TContext>(string moduleName, CancellationToken cancellationToken, bool reset = false)
		where TContext : DbContext
	{
		await using var scope = serviceProvider.CreateAsyncScope();
		var context = scope.ServiceProvider.GetRequiredService<TContext>();

		var lockId = GenerateLockId(moduleName);
		LogAcquiringLock(logger, moduleName, lockId);

		var connection = context.Database.GetDbConnection();
		await connection.OpenAsync(cancellationToken);

		try
		{
			await using var lockCmd = connection.CreateCommand();
			lockCmd.CommandText = $"SELECT pg_advisory_lock({lockId})";
			await lockCmd.ExecuteNonQueryAsync(cancellationToken);

			if (reset)
			{
				LogResettingDatabase(logger, moduleName);
				await context.Database.EnsureDeletedAsync(cancellationToken);
			}

			var pending = (await context.Database.GetPendingMigrationsAsync(cancellationToken)).ToList();

			if (pending.Count == 0)
			{
				LogNoPendingMigrations(logger, moduleName);
				return;
			}

			var migrationNames = string.Join(", ", pending);
			LogApplyingMigrations(logger, moduleName, pending.Count, migrationNames);

			await context.Database.MigrateAsync(cancellationToken);

			var appliedCount = (await context.Database.GetAppliedMigrationsAsync(cancellationToken)).Count();
			LogMigrationsApplied(logger, moduleName, appliedCount);
		}
		finally
		{
			await using var unlockCmd = connection.CreateCommand();
			unlockCmd.CommandText = $"SELECT pg_advisory_unlock({lockId})";
			await unlockCmd.ExecuteNonQueryAsync(CancellationToken.None);

			LogLockReleased(logger, moduleName);
		}
	}
}
