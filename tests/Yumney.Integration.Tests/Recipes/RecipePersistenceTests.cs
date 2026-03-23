using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SmartSolutionsLab.Yumney.Integration.Tests.Fixtures;
using SmartSolutionsLab.Yumney.Recipes.Domain.Recipe;
using SmartSolutionsLab.Yumney.Recipes.Infrastructure.Persistence;
using Xunit;

namespace SmartSolutionsLab.Yumney.Integration.Tests.Recipes;

[Collection(AspireCollection.Name)]
public class RecipePersistenceTests : IAsyncLifetime
{
    private readonly AspireFixture fixture;
    private readonly OwnerIdentifier owner = new($"persist-test-{Guid.NewGuid():N}");

    public RecipePersistenceTests(AspireFixture fixture)
    {
        this.fixture = fixture;
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync()
    {
        await using var context = await fixture.CreateRecipesDbContextAsync();
        var recipes = await context.Recipes
            .Where(r => r.Owner == owner)
            .ToListAsync();
        context.Recipes.RemoveRange(recipes);
        await context.SaveChangesAsync();
    }

    [Fact]
    public async Task AddAsync_NewRecipe_PersistsToDatabase()
    {
        var recipe = RecipeFactory.Lasagne(owner.Value);

        await using (var writeContext = await fixture.CreateRecipesDbContextAsync())
        {
            var repository = new RecipeRepository(writeContext);
            await repository.AddAsync(recipe);
        }

        await using var readContext = await fixture.CreateRecipesDbContextAsync();
        var saved = await readContext.Recipes
            .Include(r => r.Ingredients)
            .Include(r => r.Steps)
            .FirstOrDefaultAsync(r => r.Id == recipe.Id);

        saved.Should().NotBeNull();
        saved!.Title.Value.Should().Be("Classic Lasagne");
    }

    [Fact]
    public async Task AddAsync_NewRecipe_PersistsIngredients()
    {
        var recipe = RecipeFactory.Lasagne(owner.Value);

        await using (var writeContext = await fixture.CreateRecipesDbContextAsync())
        {
            var repository = new RecipeRepository(writeContext);
            await repository.AddAsync(recipe);
        }

        await using var readContext = await fixture.CreateRecipesDbContextAsync();
        var saved = await readContext.Recipes
            .Include(r => r.Ingredients)
            .FirstOrDefaultAsync(r => r.Id == recipe.Id);

        saved!.Ingredients.Should().HaveCount(10);
        saved.Ingredients.Select(i => i.Name.Value).Should().Contain("Mozzarella");
    }

    [Fact]
    public async Task AddAsync_NewRecipe_PersistsSteps()
    {
        var recipe = RecipeFactory.Lasagne(owner.Value);

        await using (var writeContext = await fixture.CreateRecipesDbContextAsync())
        {
            var repository = new RecipeRepository(writeContext);
            await repository.AddAsync(recipe);
        }

        await using var readContext = await fixture.CreateRecipesDbContextAsync();
        var saved = await readContext.Recipes
            .Include(r => r.Steps)
            .FirstOrDefaultAsync(r => r.Id == recipe.Id);

        saved!.Steps.Should().HaveCount(5);
        saved.Steps.First(s => s.Number.Value == 1).Description.Value
            .Should().Contain("Brown the ground beef");
    }

    [Fact]
    public async Task AddAsync_NewRecipe_PersistsOptionalFields()
    {
        var recipe = RecipeFactory.Lasagne(owner.Value);

        await using (var writeContext = await fixture.CreateRecipesDbContextAsync())
        {
            var repository = new RecipeRepository(writeContext);
            await repository.AddAsync(recipe);
        }

        await using var readContext = await fixture.CreateRecipesDbContextAsync();
        var saved = await readContext.Recipes.FirstOrDefaultAsync(r => r.Id == recipe.Id);

        saved!.Description!.Value.Should().Contain("Bolognese");
        saved.Servings!.Value.Should().Be(6);
    }

    [Fact]
    public async Task GetByIdAsync_ExistingRecipe_ReturnsWithRelations()
    {
        var recipe = RecipeFactory.TomatoSoup(owner.Value);

        await using (var writeContext = await fixture.CreateRecipesDbContextAsync())
        {
            var repository = new RecipeRepository(writeContext);
            await repository.AddAsync(recipe);
        }

        await using var readContext = await fixture.CreateRecipesDbContextAsync();
        var repository2 = new RecipeRepository(readContext);
        var loaded = await repository2.GetByIdAsync(recipe.Id);

        loaded.Should().NotBeNull();
        loaded!.Title.Value.Should().Be("Roasted Tomato Soup");
        loaded.Ingredients.Should().NotBeEmpty();
        loaded.Steps.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetByIdAsync_NonExistent_ReturnsNull()
    {
        await using var context = await fixture.CreateRecipesDbContextAsync();
        var repository = new RecipeRepository(context);

        var loaded = await repository.GetByIdAsync(RecipeIdentifier.New());

        loaded.Should().BeNull();
    }

    [Fact]
    public async Task UpdateAsync_ExistingRecipe_PersistsChanges()
    {
        var recipe = RecipeFactory.TomatoSoup(owner.Value);

        await using (var writeContext = await fixture.CreateRecipesDbContextAsync())
        {
            var repository = new RecipeRepository(writeContext);
            await repository.AddAsync(recipe);
        }

        await using (var updateContext = await fixture.CreateRecipesDbContextAsync())
        {
            var repository = new RecipeRepository(updateContext);
            var loaded = await repository.GetByIdAsync(recipe.Id);
            loaded!.Update(
                new RecipeTitle("Updated Tomato Soup"),
                [Ingredient.Create(new IngredientName("Cherry tomatoes"), new Amount(800), new Unit("g"))],
                [Step.Create(new StepNumber(1), new StepDescription("Roast cherry tomatoes"))],
                new RecipeDescription("Updated description"),
                new Servings(2));
            await repository.UpdateAsync(loaded);
        }

        await using var readContext = await fixture.CreateRecipesDbContextAsync();
        var updated = await readContext.Recipes
            .Include(r => r.Ingredients)
            .Include(r => r.Steps)
            .FirstOrDefaultAsync(r => r.Id == recipe.Id);

        updated!.Title.Value.Should().Be("Updated Tomato Soup");
        updated.Description!.Value.Should().Be("Updated description");
        updated.Servings!.Value.Should().Be(2);
        updated.Ingredients.Should().ContainSingle();
        updated.Ingredients[0].Name.Value.Should().Be("Cherry tomatoes");
        updated.Steps.Should().ContainSingle();
    }

    [Fact]
    public async Task DeleteAsync_ExistingRecipe_RemovesFromDatabase()
    {
        var recipe = RecipeFactory.ChocolateCake(owner.Value);

        await using (var writeContext = await fixture.CreateRecipesDbContextAsync())
        {
            var repository = new RecipeRepository(writeContext);
            await repository.AddAsync(recipe);
        }

        await using (var deleteContext = await fixture.CreateRecipesDbContextAsync())
        {
            var repository = new RecipeRepository(deleteContext);
            var loaded = await repository.GetByIdAsync(recipe.Id);
            await repository.DeleteAsync(loaded!);
        }

        await using var readContext = await fixture.CreateRecipesDbContextAsync();
        var deleted = await readContext.Recipes.FirstOrDefaultAsync(r => r.Id == recipe.Id);

        deleted.Should().BeNull();
    }

    [Fact]
    public async Task ExistsBySourceUrlAsync_ExistingUrl_ReturnsTrue()
    {
        var sourceUrl = new RecipeUrl("https://example.com/lasagne-test");
        var recipe = Recipe.Create(
            new RecipeTitle("URL Test Recipe"),
            owner,
            [Ingredient.Create(new IngredientName("Test"), null, null)],
            [Step.Create(new StepNumber(1), new StepDescription("Test"))],
            sourceUrl: sourceUrl);

        await using (var writeContext = await fixture.CreateRecipesDbContextAsync())
        {
            var repository = new RecipeRepository(writeContext);
            await repository.AddAsync(recipe);
        }

        await using var readContext = await fixture.CreateRecipesDbContextAsync();
        var repository2 = new RecipeRepository(readContext);
        var exists = await repository2.ExistsBySourceUrlAsync(sourceUrl, owner);

        exists.Should().BeTrue();
    }

    [Fact]
    public async Task ExistsBySourceUrlAsync_DifferentOwner_ReturnsFalse()
    {
        var sourceUrl = new RecipeUrl("https://example.com/owner-test");
        var recipe = Recipe.Create(
            new RecipeTitle("Owner Test Recipe"),
            owner,
            [Ingredient.Create(new IngredientName("Test"), null, null)],
            [Step.Create(new StepNumber(1), new StepDescription("Test"))],
            sourceUrl: sourceUrl);

        await using (var writeContext = await fixture.CreateRecipesDbContextAsync())
        {
            var repository = new RecipeRepository(writeContext);
            await repository.AddAsync(recipe);
        }

        await using var readContext = await fixture.CreateRecipesDbContextAsync();
        var repository2 = new RecipeRepository(readContext);
        var exists = await repository2.ExistsBySourceUrlAsync(sourceUrl, OwnerIdentifier.From("other-user"));

        exists.Should().BeFalse();
    }

    [Fact]
    public async Task ExistsBySourceUrlAsync_NonExistentUrl_ReturnsFalse()
    {
        await using var context = await fixture.CreateRecipesDbContextAsync();
        var repository = new RecipeRepository(context);

        var exists = await repository.ExistsBySourceUrlAsync(
            new RecipeUrl("https://example.com/nonexistent"),
            owner);

        exists.Should().BeFalse();
    }
}
