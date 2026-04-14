using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SmartSolutionsLab.Yumney.Recipes.Domain.Recipe;
using SmartSolutionsLab.Yumney.Recipes.Infrastructure.Persistence;
using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingList;
using SmartSolutionsLab.Yumney.Shopping.Infrastructure.Persistence;
using SmartSolutionsLab.Yumney.Users.Domain.AppUserProfile;
using SmartSolutionsLab.Yumney.Users.Infrastructure.Persistence;
using Xunit;

namespace SmartSolutionsLab.Yumney.Integration.Tests.Fixtures;

/// <summary>
/// Integration test fixture that boots the Aspire AppHost with E2ETests mode.
/// Starts infra (PostgreSQL, Keycloak, Redis, RabbitMQ) and all 4 API projects.
/// Skips frontend, gateway, mailpit, scalar, and LLM for fast startup.
/// Exposes HttpClients for each API and DbContext factories for seeding.
/// </summary>
public sealed class AspireFixture : IAsyncLifetime
{
    private static readonly TimeSpan StartupTimeout = TimeSpan.FromMinutes(3);

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

    public DistributedApplication App => app ?? throw new InvalidOperationException("Aspire app not started");

    /// <summary>Gets the pre-configured HttpClient targeting the Recipes API.</summary>
    public HttpClient RecipesApi { get; private set; } = null!;

    /// <summary>Gets the pre-configured HttpClient targeting the Shopping API.</summary>
    public HttpClient ShoppingApi { get; private set; } = null!;

    /// <summary>Gets the pre-configured HttpClient targeting the Users API.</summary>
    public HttpClient UsersApi { get; private set; } = null!;

    /// <summary>Gets the pre-configured HttpClient targeting the MealPlan API.</summary>
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
                "E2ETests=true",
            ]);

        builder.Services.ConfigureHttpClientDefaults(http => http.AddStandardResilienceHandler());

        app = await builder.BuildAsync();

        using var cts = new CancellationTokenSource(StartupTimeout);
        await app.StartAsync(cts.Token);

        await app.ResourceNotifications.WaitForResourceAsync("recipes-api", KnownResourceStates.Running, cts.Token);
        await app.ResourceNotifications.WaitForResourceAsync("shopping-api", KnownResourceStates.Running, cts.Token);
        await app.ResourceNotifications.WaitForResourceAsync("users-api", KnownResourceStates.Running, cts.Token);
        await app.ResourceNotifications.WaitForResourceAsync("mealplan-api", KnownResourceStates.Running, cts.Token);

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

    public async Task SeedRecipesAsync(params Recipe[] recipes)
    {
        await using var context = await CreateRecipesDbContextAsync();
        context.Recipes.AddRange(recipes);
        await context.SaveChangesAsync();
    }

    public async Task<ShoppingDbContext> CreateShoppingDbContextAsync()
    {
        var connectionString = await App.GetConnectionStringAsync("shoppingdb");
        var optionsBuilder = new DbContextOptionsBuilder<ShoppingDbContext>();
        optionsBuilder.UseNpgsql(connectionString, x => x.EnableRetryOnFailure());
        return new ShoppingDbContext(optionsBuilder.Options);
    }

    public async Task SeedShoppingListsAsync(params ShoppingList[] shoppingLists)
    {
        await using var context = await CreateShoppingDbContextAsync();
        context.ShoppingLists.AddRange(shoppingLists);
        await context.SaveChangesAsync();
    }

    public async Task<UsersDbContext> CreateUsersDbContextAsync()
    {
        var connectionString = await App.GetConnectionStringAsync("usersdb");
        var optionsBuilder = new DbContextOptionsBuilder<UsersDbContext>();
        optionsBuilder.UseNpgsql(connectionString, x => x.EnableRetryOnFailure());
        return new UsersDbContext(optionsBuilder.Options);
    }

    public async Task SeedUserProfilesAsync(params AppUserProfile[] profiles)
    {
        await using var context = await CreateUsersDbContextAsync();
        context.AppUserProfiles.AddRange(profiles);
        await context.SaveChangesAsync();
    }
}

[CollectionDefinition(Name)]
#pragma warning disable CA1711 // xUnit convention requires Collection suffix
public class AspireCollection : ICollectionFixture<AspireFixture>
{
    public const string Name = "Aspire";
}
