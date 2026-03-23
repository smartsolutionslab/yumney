using Aspire.Hosting;
using Aspire.Hosting.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SmartSolutionsLab.Yumney.Recipes.Domain.Recipe;
using SmartSolutionsLab.Yumney.Recipes.Infrastructure.Persistence;
using Xunit;

namespace SmartSolutionsLab.Yumney.Integration.Tests.Fixtures;

public sealed class AspireFixture : IAsyncLifetime
{
    private DistributedApplication? app;

    public DistributedApplication App => app ?? throw new InvalidOperationException("Aspire app not started");

    public async Task InitializeAsync()
    {
        var builder = await DistributedApplicationTestingBuilder
            .CreateAsync<Projects.Yumney_AppHost>();

        builder.Services.ConfigureHttpClientDefaults(http =>
            http.AddStandardResilienceHandler());

        app = await builder.BuildAsync();
        await app.StartAsync();

        // Allow time for migration runner to complete and APIs to become ready
        await Task.Delay(TimeSpan.FromSeconds(15));
    }

    public async Task DisposeAsync()
    {
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
}

[CollectionDefinition(Name)]
#pragma warning disable CA1711 // xUnit convention requires Collection suffix
public class AspireCollection : ICollectionFixture<AspireFixture>
{
    public const string Name = "Aspire";
}
