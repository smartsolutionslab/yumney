using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SmartSolutionsLab.Yumney.Integration.Tests.Fixtures;
using SmartSolutionsLab.Yumney.Recipes.Domain.Recipe;
using SmartSolutionsLab.Yumney.Recipes.Infrastructure.Persistence;
using Xunit;

namespace SmartSolutionsLab.Yumney.Integration.Tests.Recipes;

[Collection(AspireCollection.Name)]
public class RecipePersistenceTests(AspireFixture fixture) : IAsyncLifetime
{
    private readonly OwnerIdentifier owner = OwnerIdentifier.From($"persist-test-{Guid.NewGuid():N}");

    public Task InitializeAsync() => Task.CompletedTask;

    public Task DisposeAsync() => AspireFixture.CleanupAsync(
        fixture.CreateRecipesDbContextAsync,
        ctx => ctx.Recipes.Where(r => r.Owner == owner));

    [Fact]
    public async Task AddAsync_NewRecipe_PersistsWithAllRelationsAndOptionalFields()
    {
        var recipe = RecipeFactory.Lasagne(owner.Value);

        await using (var writeContext = await fixture.CreateRecipesDbContextAsync())
        {
            var recipes = new RecipeRepository(writeContext);
            await recipes.AddAsync(recipe);
        }

        await using var readContext = await fixture.CreateRecipesDbContextAsync();
        var saved = await readContext.Recipes
            .Include(r => r.Ingredients)
            .Include(r => r.Steps)
            .FirstOrDefaultAsync(r => r.Id == recipe.Id);

        saved.Should().NotBeNull();
        saved!.Title.Value.Should().Be("Classic Lasagne");
        saved.Description!.Value.Should().Contain("Bolognese");
        saved.Servings!.Value.Should().Be(6);
        saved.Ingredients.Should().HaveCount(10);
        saved.Ingredients.Select(i => i.Name.Value).Should().Contain("Mozzarella");
        saved.Steps.Should().HaveCount(5);
        saved.Steps.First(s => s.Number.Value == 1).Description.Value
            .Should().Contain("Brown the ground beef");
    }

    [Fact]
    public async Task GetByIdAsync_ExistingRecipe_ReturnsWithRelations()
    {
        var recipe = RecipeFactory.TomatoSoup(owner.Value);
        await fixture.SeedRecipesAsync(recipe);

        await using var readContext = await fixture.CreateRecipesDbContextAsync();
        var recipes = new RecipeRepository(readContext);
        var loaded = await recipes.GetByIdAsync(recipe.Id);

        loaded.Should().NotBeNull();
        loaded!.Title.Value.Should().Be("Roasted Tomato Soup");
        loaded.Ingredients.Should().NotBeEmpty();
        loaded.Steps.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetByIdAsync_NonExistent_ReturnsNull()
    {
        await using var context = await fixture.CreateRecipesDbContextAsync();
        var recipes = new RecipeRepository(context);

        var loaded = await recipes.GetByIdAsync(RecipeIdentifier.New());

        loaded.Should().BeNull();
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsDetachedEntity()
    {
        var recipe = RecipeFactory.TomatoSoup(owner.Value);
        await fixture.SeedRecipesAsync(recipe);

        await using var readContext = await fixture.CreateRecipesDbContextAsync();
        var recipes = new RecipeRepository(readContext);
        var loaded = await recipes.GetByIdAsync(recipe.Id);

        readContext.Entry(loaded!).State.Should().Be(EntityState.Detached);
    }

    [Fact]
    public async Task GetByIdForUpdateAsync_ReturnsTrackedEntity()
    {
        var recipe = RecipeFactory.TomatoSoup(owner.Value);
        await fixture.SeedRecipesAsync(recipe);

        await using var readContext = await fixture.CreateRecipesDbContextAsync();
        var recipes = new RecipeRepository(readContext);
        var loaded = await recipes.GetByIdForUpdateAsync(recipe.Id);

        readContext.Entry(loaded!).State.Should().Be(EntityState.Unchanged);
    }

    [Fact]
    public async Task UpdateAsync_ExistingRecipe_PersistsChanges()
    {
        var recipe = RecipeFactory.TomatoSoup(owner.Value);
        await fixture.SeedRecipesAsync(recipe);

        await using (var updateContext = await fixture.CreateRecipesDbContextAsync())
        {
            var recipes = new RecipeRepository(updateContext);
            var loaded = await recipes.GetByIdForUpdateAsync(recipe.Id);
            loaded!.Update(
                RecipeTitle.From("Updated Tomato Soup"),
                [Ingredient.Create(IngredientName.From("Cherry tomatoes"), Quantity.Of(Amount.From(800), Unit.From("g")))],
                [Step.Create(StepNumber.From(1), StepDescription.From("Roast cherry tomatoes"))],
                RecipeDescription.From("Updated description"),
                Servings.From(2));
            await recipes.UpdateAsync(loaded);
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
        await fixture.SeedRecipesAsync(recipe);

        await using (var deleteContext = await fixture.CreateRecipesDbContextAsync())
        {
            var recipes = new RecipeRepository(deleteContext);
            var loaded = await recipes.GetByIdForUpdateAsync(recipe.Id);
            await recipes.DeleteAsync(loaded!);
        }

        await using var readContext = await fixture.CreateRecipesDbContextAsync();
        var deleted = await readContext.Recipes.FirstOrDefaultAsync(r => r.Id == recipe.Id);

        deleted.Should().BeNull();
    }

    [Fact]
    public async Task ExistsBySourceUrlAsync_ExistingUrl_ReturnsTrue()
    {
        var sourceUrl = RecipeUrl.From("https://example.com/lasagne-test");
        var recipe = Recipe.Create(
            RecipeTitle.From("URL Test Recipe"),
            owner,
            [Ingredient.Create(IngredientName.From("Test"), null)],
            [Step.Create(StepNumber.From(1), StepDescription.From("Test"))],
            sourceUrl: sourceUrl);
        await fixture.SeedRecipesAsync(recipe);

        await using var readContext = await fixture.CreateRecipesDbContextAsync();
        var recipes = new RecipeRepository(readContext);
        var exists = await recipes.ExistsBySourceUrlAsync(sourceUrl, owner);

        exists.Should().BeTrue();
    }

    [Fact]
    public async Task ExistsBySourceUrlAsync_DifferentOwner_ReturnsFalse()
    {
        var sourceUrl = RecipeUrl.From("https://example.com/owner-test");
        var recipe = Recipe.Create(
            RecipeTitle.From("Owner Test Recipe"),
            owner,
            [Ingredient.Create(IngredientName.From("Test"), null)],
            [Step.Create(StepNumber.From(1), StepDescription.From("Test"))],
            sourceUrl: sourceUrl);
        await fixture.SeedRecipesAsync(recipe);

        await using var readContext = await fixture.CreateRecipesDbContextAsync();
        var recipes = new RecipeRepository(readContext);
        var exists = await recipes.ExistsBySourceUrlAsync(sourceUrl, OwnerIdentifier.From("other-user"));

        exists.Should().BeFalse();
    }

    [Fact]
    public async Task ExistsBySourceUrlAsync_NonExistentUrl_ReturnsFalse()
    {
        await using var context = await fixture.CreateRecipesDbContextAsync();
        var recipes = new RecipeRepository(context);

        var exists = await recipes.ExistsBySourceUrlAsync(
            RecipeUrl.From("https://example.com/nonexistent"),
            owner);

        exists.Should().BeFalse();
    }
}
