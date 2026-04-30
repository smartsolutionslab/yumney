using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SmartSolutionsLab.Yumney.Integration.Tests.Fixtures;
using SmartSolutionsLab.Yumney.Recipes.Domain.Recipe;
using SmartSolutionsLab.Yumney.Recipes.Infrastructure.Persistence;
using SmartSolutionsLab.Yumney.Shared.Common;
using Xunit;

namespace SmartSolutionsLab.Yumney.Integration.Tests.Recipes;

[Collection(AspireCollection.Name)]
public class RecipePersistenceTests(AspireFixture fixture) : IAsyncLifetime
{
	private readonly OwnerIdentifier owner = OwnerIdentifier.From($"persist-test-{Guid.NewGuid():N}");

	public Task InitializeAsync() => Task.CompletedTask;

	public Task DisposeAsync() => AspireFixture.CleanupAsync(
		fixture.CreateRecipesDbContextAsync,
		ctx => ctx.Recipes.Where(recipe => recipe.Owner == owner));

	[Fact]
	public async Task AddAsync_NewRecipe_PersistsWithAllRelationsAndOptionalFields()
	{
		var recipe = RecipeFactory.Lasagne(owner.Value);

		await using (var writeContext = await fixture.CreateRecipesDbContextAsync())
		{
			var recipes = new RecipeRepository(writeContext);
			await recipes.AddAsync(recipe);
			await writeContext.SaveChangesAsync();
		}

		await using var readContext = await fixture.CreateRecipesDbContextAsync();
		var saved = await readContext.Recipes
			.Include(r => r.Ingredients)
			.Include(r => r.Steps)
			.FirstOrDefaultAsync(r => r.Id == recipe.Id);

		saved.Should().NotBeNull();
		saved!.Title.Should().Be(RecipeTitle.From("Classic Lasagne"));
		saved.Description!.Value.Should().Contain("Bolognese");
		saved.Servings.Should().Be(Servings.From(6));
		saved.Ingredients.Should().HaveCount(10);
		saved.Ingredients.Select(ingredient => ingredient.Name).Should().Contain(IngredientName.From("Mozzarella"));
		saved.Steps.Should().HaveCount(5);
		saved.Steps.First(s => s.Number == StepNumber.From(1)).Description.Value
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

		loaded.Title.Should().Be(RecipeTitle.From("Roasted Tomato Soup"));
		loaded.Ingredients.Should().NotBeEmpty();
		loaded.Steps.Should().NotBeEmpty();
	}

	[Fact]
	public async Task GetByIdAsync_NonExistent_ThrowsEntityNotFoundException()
	{
		await using var context = await fixture.CreateRecipesDbContextAsync();
		var recipes = new RecipeRepository(context);

		var act = () => recipes.GetByIdAsync(RecipeIdentifier.New());

		await act.Should().ThrowAsync<EntityNotFoundException>();
	}

	[Fact]
	public async Task GetByIdAsync_ReturnsDetachedEntity()
	{
		var recipe = RecipeFactory.TomatoSoup(owner.Value);
		await fixture.SeedRecipesAsync(recipe);

		await using var readContext = await fixture.CreateRecipesDbContextAsync();
		var recipes = new RecipeRepository(readContext);
		var loaded = await recipes.GetByIdAsync(recipe.Id);

		readContext.Entry(loaded).State.Should().Be(EntityState.Detached);
	}

	[Fact]
	public async Task GetByIdForUpdateAsync_ReturnsTrackedEntity()
	{
		var recipe = RecipeFactory.TomatoSoup(owner.Value);
		await fixture.SeedRecipesAsync(recipe);

		await using var readContext = await fixture.CreateRecipesDbContextAsync();
		var recipes = new RecipeRepository(readContext);
		var loaded = await recipes.GetByIdForUpdateAsync(recipe.Id);

		readContext.Entry(loaded).State.Should().Be(EntityState.Unchanged);
	}

	[Fact]
	public async Task GetByIdAsync_RecipeWithTags_LoadsTags()
	{
		var recipe = RecipeFactory.Lasagne(owner.Value);
		await fixture.SeedRecipesAsync(recipe);

		await using var readContext = await fixture.CreateRecipesDbContextAsync();
		var recipes = new RecipeRepository(readContext);
		var loaded = await recipes.GetByIdAsync(recipe.Id);

		loaded.Tags.Select(tag => tag.Value).Should().BeEquivalentTo(["italian", "pasta", "comfort-food"]);
	}

	[Fact]
	public async Task GetAllByOwnerWithIngredientsAsync_ReturnsRecipesWithIngredientsLoaded()
	{
		var lasagne = RecipeFactory.Lasagne(owner.Value);
		var soup = RecipeFactory.TomatoSoup(owner.Value);
		await fixture.SeedRecipesAsync(lasagne, soup);

		await using var readContext = await fixture.CreateRecipesDbContextAsync();
		var recipes = new RecipeRepository(readContext);
		var loaded = await recipes.GetAllByOwnerWithIngredientsAsync(owner);

		loaded.Should().HaveCount(2);
		loaded.Should().AllSatisfy(r => r.Ingredients.Should().NotBeEmpty());
	}

	[Fact]
	public async Task GetAllByOwnerWithIngredientsAsync_OtherOwnersRecipes_NotIncluded()
	{
		var mine = RecipeFactory.Lasagne(owner.Value);
		var otherOwner = OwnerIdentifier.From($"other-{Guid.NewGuid():N}");
		var theirs = RecipeFactory.TomatoSoup(otherOwner.Value);
		try
		{
			await fixture.SeedRecipesAsync(mine, theirs);

			await using var readContext = await fixture.CreateRecipesDbContextAsync();
			var recipes = new RecipeRepository(readContext);
			var loaded = await recipes.GetAllByOwnerWithIngredientsAsync(owner);

			loaded.Should().ContainSingle().Which.Id.Should().Be(mine.Id);
		}
		finally
		{
			await AspireFixture.CleanupAsync(
				fixture.CreateRecipesDbContextAsync,
				ctx => ctx.Recipes.Where(recipe => recipe.Owner == otherOwner));
		}
	}

	[Fact]
	public async Task GetAllByOwnerWithIngredientsAsync_NoRecipes_ReturnsEmpty()
	{
		await using var context = await fixture.CreateRecipesDbContextAsync();
		var recipes = new RecipeRepository(context);

		var loaded = await recipes.GetAllByOwnerWithIngredientsAsync(owner);

		loaded.Should().BeEmpty();
	}

	[Fact]
	public async Task GetByOwnerAsync_RecipeWithTags_LoadsTagsForListItems()
	{
		var recipe = RecipeFactory.Lasagne(owner.Value);
		await fixture.SeedRecipesAsync(recipe);
		var paging = PagingOptions.From(1, 20);
		var sorting = new SortingOptions<RecipeSortField>(RecipeSortField.Date, SortDirection.Descending);

		await using var readContext = await fixture.CreateRecipesDbContextAsync();
		var recipes = new RecipeRepository(readContext);
		var (items, _) = await recipes.GetByOwnerAsync(owner, paging, sorting);

		items.Single().Tags.Select(tag => tag.Value).Should().BeEquivalentTo(["italian", "pasta", "comfort-food"]);
	}

	[Fact]
	public async Task UpdateAsync_ExistingRecipe_PersistsChanges()
	{
		var recipe = RecipeFactory.TomatoSoup(owner.Value);
		await fixture.SeedRecipesAsync(recipe);

		var updatedTitle = RecipeTitle.From("Updated Tomato Soup");
		var updatedDescription = RecipeDescription.From("Updated description");
		var updatedServings = Servings.From(2);
		var ingredientName = IngredientName.From("Cherry tomatoes");

		await using (var updateContext = await fixture.CreateRecipesDbContextAsync())
		{
			var recipes = new RecipeRepository(updateContext);
			var loaded = await recipes.GetByIdForUpdateAsync(recipe.Id);
			loaded.Update(
				updatedTitle,
				[Ingredient.Create(ingredientName, Quantity.Of(Amount.From(800), Unit.Gram))],
				[Step.Create(StepNumber.From(1), StepDescription.From("Roast cherry tomatoes"))],
				updatedDescription,
				updatedServings);
			await updateContext.SaveChangesAsync();
		}

		await using var readContext = await fixture.CreateRecipesDbContextAsync();
		var updated = await readContext.Recipes
			.Include(r => r.Ingredients)
			.Include(r => r.Steps)
			.FirstOrDefaultAsync(r => r.Id == recipe.Id);

		updated!.Title.Should().Be(updatedTitle);
		updated.Description.Should().Be(updatedDescription);
		updated.Servings.Should().Be(updatedServings);
		updated.Ingredients.Should().ContainSingle();
		updated.Ingredients[0].Name.Should().Be(ingredientName);
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
			recipes.Remove(loaded);
			await deleteContext.SaveChangesAsync();
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
