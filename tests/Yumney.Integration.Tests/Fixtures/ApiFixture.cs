using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SmartSolutionsLab.Yumney.MealPlan.Infrastructure.Persistence;
using SmartSolutionsLab.Yumney.Recipes.Infrastructure.Persistence;
using SmartSolutionsLab.Yumney.Shopping.Infrastructure.Persistence;
using SmartSolutionsLab.Yumney.Users.Infrastructure.Persistence;
using Xunit;

namespace SmartSolutionsLab.Yumney.Integration.Tests.Fixtures;

/// <summary>
/// Full-stack integration test fixture that boots the Aspire AppHost
/// and exposes typed HttpClients for each API resource.
/// Uses real PostgreSQL, Keycloak, Redis, and RabbitMQ via Docker.
/// </summary>
public sealed class ApiFixture : IAsyncLifetime
{
    private static readonly TimeSpan StartupTimeout = TimeSpan.FromMinutes(5);

    public static async Task CleanupAsync<TContext>(
        Func<Task<TContext>> contextFactory,
        Func<TContext, IQueryable<object>> query)
        where TContext : DbContext
    {
        await using var context = await contextFactory();
        var entities = await query(context).ToListAsync();
        context.RemoveRange(entities);
        await context.SaveChangesAsync();
    }

    private DistributedApplication? app;

    public DistributedApplication App => app ?? throw new InvalidOperationException("AppHost not started");

    /// <summary>Pre-configured HttpClient targeting the Recipes API.</summary>
    public HttpClient RecipesApi { get; private set; } = null!;

    /// <summary>Pre-configured HttpClient targeting the Shopping API.</summary>
    public HttpClient ShoppingApi { get; private set; } = null!;

    /// <summary>Pre-configured HttpClient targeting the Users API.</summary>
    public HttpClient UsersApi { get; private set; } = null!;

    /// <summary>Pre-configured HttpClient targeting the MealPlan API.</summary>
    public HttpClient MealPlanApi { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        var builder = await DistributedApplicationTestingBuilder
            .CreateAsync<Projects.Yumney_AppHost>(
            [
                "Parameters:PostgresUser=testuser",
                "Parameters:PostgresPassword=testpassword",
                "Parameters:KeycloakPassword=testkeycloak",
                "Parameters:MessagingPassword=testmessaging",
                "Parameters:RedisPassword=testredis",
            ]);

        builder.Services.ConfigureHttpClientDefaults(http => http.AddStandardResilienceHandler());

        app = await builder.BuildAsync();

        using var cts = new CancellationTokenSource(StartupTimeout);
        await app.StartAsync(cts.Token);

        // Wait for all API resources to be running
        await app.ResourceNotifications.WaitForResourceAsync("recipes-api", KnownResourceStates.Running, cts.Token);
        await app.ResourceNotifications.WaitForResourceAsync("shopping-api", KnownResourceStates.Running, cts.Token);
        await app.ResourceNotifications.WaitForResourceAsync("users-api", KnownResourceStates.Running, cts.Token);
        await app.ResourceNotifications.WaitForResourceAsync("mealplan-api", KnownResourceStates.Running, cts.Token);

        // Create HttpClients routed to each API via Aspire's service discovery
        RecipesApi = app.CreateHttpClient("recipes-api");
        ShoppingApi = app.CreateHttpClient("shopping-api");
        UsersApi = app.CreateHttpClient("users-api");
        MealPlanApi = app.CreateHttpClient("mealplan-api");
    }

    public async Task DisposeAsync()
    {
        RecipesApi?.Dispose();
        ShoppingApi?.Dispose();
        UsersApi?.Dispose();
        MealPlanApi?.Dispose();

        if (app is not null)
        {
            await app.StopAsync();
            await ((IAsyncDisposable)app).DisposeAsync();
        }
    }

    public async Task<RecipesDbContext> CreateRecipesDbContextAsync()
    {
        var connectionString = await App.GetConnectionStringAsync("recipesdb");
        var optionsBuilder = new DbContextOptionsBuilder<RecipesDbContext>();
        optionsBuilder.UseNpgsql(connectionString, x => x.EnableRetryOnFailure());
        return new RecipesDbContext(optionsBuilder.Options);
    }

    public async Task<ShoppingDbContext> CreateShoppingDbContextAsync()
    {
        var connectionString = await App.GetConnectionStringAsync("shoppingdb");
        var optionsBuilder = new DbContextOptionsBuilder<ShoppingDbContext>();
        optionsBuilder.UseNpgsql(connectionString, x => x.EnableRetryOnFailure());
        return new ShoppingDbContext(optionsBuilder.Options);
    }

    public async Task<UsersDbContext> CreateUsersDbContextAsync()
    {
        var connectionString = await App.GetConnectionStringAsync("usersdb");
        var optionsBuilder = new DbContextOptionsBuilder<UsersDbContext>();
        optionsBuilder.UseNpgsql(connectionString, x => x.EnableRetryOnFailure());
        return new UsersDbContext(optionsBuilder.Options);
    }

    public async Task<MealPlanDbContext> CreateMealPlanDbContextAsync()
    {
        var connectionString = await App.GetConnectionStringAsync("mealplandb");
        var optionsBuilder = new DbContextOptionsBuilder<MealPlanDbContext>();
        optionsBuilder.UseNpgsql(connectionString, x => x.EnableRetryOnFailure());
        return new MealPlanDbContext(optionsBuilder.Options);
    }
}

[CollectionDefinition(Name)]
#pragma warning disable CA1711
public class ApiCollection : ICollectionFixture<ApiFixture>
{
    public const string Name = "Api";
}
#pragma warning restore CA1711
