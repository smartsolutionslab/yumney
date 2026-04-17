using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
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
    private static readonly TimeSpan StartupTimeout = TimeSpan.FromMinutes(8);

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
        await CleanupStaleContainersAsync();

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

        try
        {
            await app.StartAsync(cts.Token);

            await app.ResourceNotifications.WaitForResourceAsync(
                "keycloak", KnownResourceStates.Running, cts.Token);
            await app.ResourceNotifications.WaitForResourceAsync(
                "yumney-migrations", KnownResourceStates.Finished, cts.Token);

            var apis = new[] { "recipes-api", "shopping-api", "users-api", "mealplan-api" };
            foreach (var api in apis)
            {
                await app.ResourceNotifications.WaitForResourceAsync(api, KnownResourceStates.Running, cts.Token);
            }

            // Keycloak realm import may still be in progress after container is Running.
            // Verify the token endpoint is reachable before proceeding.
            var keycloakClient = app.CreateHttpClient("keycloak");
            for (var i = 0; i < 30; i++)
            {
                try
                {
                    var probe = await keycloakClient.GetAsync(
                        "/realms/yumney/.well-known/openid-configuration", cts.Token);
                    if (probe.IsSuccessStatusCode) break;
                }
                catch
                {
                    // Not ready yet
                }

                await Task.Delay(1000, cts.Token);
            }
        }
        catch (Exception ex) when (ex is OperationCanceledException or TaskCanceledException)
        {
            throw new TimeoutException(
                $"Aspire AppHost did not start within {StartupTimeout.TotalMinutes} minutes. " +
                $"Check for port conflicts (docker ps) or stale containers. Inner: {ex.Message}",
                ex);
        }

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

    public async Task<HttpClient> CreateAuthenticatedClientAsync(string resourceName)
    {
        var keycloakClient = App.CreateHttpClient("keycloak");
        var tokenResponse = await keycloakClient.PostAsync(
            "/realms/yumney/protocol/openid-connect/token",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "password",
                ["client_id"] = "yumney-web",
                ["username"] = "testuser",
                ["password"] = "Test1234",
            }));

        tokenResponse.EnsureSuccessStatusCode();
        var tokenJson = await tokenResponse.Content.ReadFromJsonAsync<JsonElement>();
        var accessToken = tokenJson.GetProperty("access_token").GetString()!;

        var client = App.CreateHttpClient(resourceName);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        return client;
    }

    private static async Task CleanupStaleContainersAsync()
    {
        try
        {
            var process = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = "docker",
                Arguments = "ps -aq --filter label=com.microsoft.developer.usvc-dev.build",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            });

            if (process is null) return;

            var output = await process.StandardOutput.ReadToEndAsync();
            await process.WaitForExitAsync();

            var ids = output.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (ids.Length == 0) return;

            var rmProcess = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = "docker",
                Arguments = $"rm -f {string.Join(' ', ids)}",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            });

            if (rmProcess is not null)
                await rmProcess.WaitForExitAsync();
        }
        catch
        {
            // Docker not available — safe to ignore
        }
    }
}
