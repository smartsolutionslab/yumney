using Microsoft.EntityFrameworkCore;
using Yumney.Recipes.Infrastructure.Persistence;
using Yumney.Users.Infrastructure.Persistence;

namespace Yumney.MigrationRunner;

/// <summary>
/// Background worker that applies EF Core migrations for all modules on startup.
/// </summary>
public sealed partial class MigrationWorker(
    IServiceProvider serviceProvider,
    IHostApplicationLifetime lifetime,
    ILogger<MigrationWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await ApplyMigrationsAsync<RecipesDbContext>("Recipes", stoppingToken);
            await ApplyMigrationsAsync<UsersDbContext>("Users", stoppingToken);

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

    private async Task ApplyMigrationsAsync<TContext>(string moduleName, CancellationToken cancellationToken)
        where TContext : DbContext
    {
        await using var scope = serviceProvider.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<TContext>();

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
}
