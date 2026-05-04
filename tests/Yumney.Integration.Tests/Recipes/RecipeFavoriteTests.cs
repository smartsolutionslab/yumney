using FluentAssertions;
using SmartSolutionsLab.Yumney.Integration.Tests.Fixtures;
using SmartSolutionsLab.Yumney.Recipes.Domain.Recipe;
using SmartSolutionsLab.Yumney.Recipes.Domain.RecipeFavorite;
using SmartSolutionsLab.Yumney.Recipes.Infrastructure.Persistence;
using SmartSolutionsLab.Yumney.Shared.Outcomes;
using SmartSolutionsLab.Yumney.Shared.Paging;
using Xunit;

namespace SmartSolutionsLab.Yumney.Integration.Tests.Recipes;

[Collection(AspireCollection.Name)]
public class RecipeFavoriteTests(AspireFixture fixture) : IAsyncLifetime
{
	private static readonly PagingOptions DefaultPaging = PagingOptions.Of(Page.From(1), PageSize.From(20));
	private static readonly SortingOptions<RecipeSortField> DefaultSorting = new(RecipeSortField.Date, SortDirection.Descending);

	private readonly OwnerIdentifier owner = OwnerIdentifier.From($"fav-test-{Guid.NewGuid():N}");
	private Recipe lasagne = default!;
	private Recipe soup = default!;

	public async Task InitializeAsync()
	{
		lasagne = RecipeFactory.Lasagne(owner.Value);
		soup = RecipeFactory.TomatoSoup(owner.Value);
		await fixture.SeedRecipesAsync(lasagne, soup);
	}

	public Task DisposeAsync() => AspireFixture.CleanupAsync(
		fixture.CreateRecipesDbContextAsync,
		ctx => ctx.Recipes.Where(recipe => recipe.Owner == owner));

	[Fact]
	public async Task IsFavoritedAsync_NotFavorited_ReturnsFalse()
	{
		await using var context = await fixture.CreateRecipesDbContextAsync();
		var favorites = new RecipeFavoriteRepository(context);

		var result = await favorites.IsFavoritedAsync(owner, lasagne.Id);

		result.Should().BeFalse();
	}

	[Fact]
	public async Task AddAsync_ThenIsFavoritedAsync_ReturnsTrue()
	{
		await using var context = await fixture.CreateRecipesDbContextAsync();
		var favorites = new RecipeFavoriteRepository(context);

		await favorites.AddAsync(RecipeFavorite.Create(lasagne.Id, owner));
		await context.SaveChangesAsync();
		var result = await favorites.IsFavoritedAsync(owner, lasagne.Id);

		result.Should().BeTrue();
	}

	[Fact]
	public async Task RemoveAsync_AfterAdd_RemovesFavorite()
	{
		await using var context = await fixture.CreateRecipesDbContextAsync();
		var favorites = new RecipeFavoriteRepository(context);

		await favorites.AddAsync(RecipeFavorite.Create(lasagne.Id, owner));
		await context.SaveChangesAsync();
		await favorites.RemoveAsync(owner, lasagne.Id);
		var result = await favorites.IsFavoritedAsync(owner, lasagne.Id);

		result.Should().BeFalse();
	}

	[Fact]
	public async Task GetFavoritedIdsAsync_ReturnsOnlyFavoritedIds()
	{
		await using var context = await fixture.CreateRecipesDbContextAsync();
		var favorites = new RecipeFavoriteRepository(context);

		await favorites.AddAsync(RecipeFavorite.Create(lasagne.Id, owner));
		await context.SaveChangesAsync();
		var ids = await favorites.GetFavoritedIdsAsync(owner, [lasagne.Id, soup.Id]);

		ids.Should().Contain(lasagne.Id.Value);
		ids.Should().NotContain(soup.Id.Value);
	}

	[Fact]
	public async Task RecipeRepository_FavoritesOnlyFilter_ReturnsOnlyFavorited()
	{
		await using var context = await fixture.CreateRecipesDbContextAsync();
		var recipes = new RecipeRepository(context);
		var favorites = new RecipeFavoriteRepository(context);

		await favorites.AddAsync(RecipeFavorite.Create(lasagne.Id, owner));
		await context.SaveChangesAsync();

		var (items, totalCount) = await recipes.GetByOwnerAsync(
			owner,
			DefaultPaging,
			DefaultSorting,
			search: null,
			filter: new RecipeFilter(FavoritesOnly: true));

		totalCount.Should().Be(ItemCount.From(1));
		items.Should().ContainSingle(r => r.Id == lasagne.Id);
	}

	[Fact]
	public async Task RecipeRepository_FavoritesOnlyFalse_ReturnsAll()
	{
		await using var context = await fixture.CreateRecipesDbContextAsync();
		var recipes = new RecipeRepository(context);

		var (_, totalCount) = await recipes.GetByOwnerAsync(
			owner,
			DefaultPaging,
			DefaultSorting,
			search: null,
			filter: new RecipeFilter(FavoritesOnly: false));

		totalCount.Should().Be(ItemCount.From(2));
	}
}
