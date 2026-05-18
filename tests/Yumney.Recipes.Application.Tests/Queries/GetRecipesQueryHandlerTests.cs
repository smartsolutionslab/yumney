using FluentAssertions;
using NSubstitute;
using SmartSolutionsLab.Yumney.Recipes.Application.Queries;
using SmartSolutionsLab.Yumney.Recipes.Application.Queries.Handlers;
using SmartSolutionsLab.Yumney.Recipes.Domain.Recipe;
using SmartSolutionsLab.Yumney.Recipes.Domain.RecipeFavorite;
using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shared.Paging;
using Xunit;

namespace SmartSolutionsLab.Yumney.Recipes.Application.Tests.Queries;

#pragma warning disable SA1601
public partial class GetRecipesQueryHandlerTests
#pragma warning restore SA1601
{
	private readonly IRecipeRepository recipes = Substitute.For<IRecipeRepository>();
	private readonly IRecipeFavoriteRepository favorites = Substitute.For<IRecipeFavoriteRepository>();
	private readonly ICurrentUser currentUser = Substitute.For<ICurrentUser>();
	private readonly GetRecipesQueryHandler handler;

	public GetRecipesQueryHandlerTests()
	{
		currentUser.UserId.Returns("user-123");
		favorites
			.GetFavoritedIdsAsync(Arg.Any<OwnerIdentifier>(), Arg.Any<IReadOnlyCollection<RecipeIdentifier>>(), Arg.Any<CancellationToken>())
			.Returns((IReadOnlySet<Guid>)new HashSet<Guid>());
		handler = new GetRecipesQueryHandler(recipes, favorites, currentUser);
	}

	[Fact]
	public async Task HandleAsync_NoRecipes_ReturnsSuccessResult()
	{
		SetupEmptyRepository();

		var query = CreateQuery(1, 20, RecipeSortField.Date, SortDirection.Descending);
		var result = await handler.HandleAsync(query);

		result.IsSuccess.Should().BeTrue();
	}

	[Fact]
	public async Task HandleAsync_NoRecipes_ReturnsEmptyItems()
	{
		SetupEmptyRepository();

		var query = CreateQuery(1, 20, RecipeSortField.Date, SortDirection.Descending);
		var result = await handler.HandleAsync(query);

		result.Value.Items.Should().BeEmpty();
		result.Value.TotalCount.Should().Be(0);
	}

	[Fact]
	public async Task HandleAsync_WithRecipes_ReturnsMappedDtos()
	{
		var recipe = RecipeTestData.CreateRecipe(title: "Pasta Carbonara");
		SetupRepository([recipe], 1);

		var query = CreateQuery(1, 20, RecipeSortField.Date, SortDirection.Descending);
		var result = await handler.HandleAsync(query);

		result.Value.Items.Should().HaveCount(1);
		result.Value.Items[0].Title.Should().Be("Pasta Carbonara");
		result.Value.Items[0].Identifier.Should().Be(recipe.Id.Value);
	}

	[Fact]
	public async Task HandleAsync_WithRecipes_ReturnsMappedCreatedAt()
	{
		var recipe = RecipeTestData.CreateRecipe(title: "Pasta Carbonara");
		SetupRepository([recipe], 1);

		var query = CreateQuery(1, 20, RecipeSortField.Date, SortDirection.Descending);
		var result = await handler.HandleAsync(query);

		result.Value.Items[0].CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
	}

	[Fact]
	public async Task HandleAsync_PageThreeWithTenPerPage_ReturnsPaginationMetadata()
	{
		SetupRepository([], 50);

		var query = CreateQuery(3, 10, RecipeSortField.Date, SortDirection.Descending);
		var result = await handler.HandleAsync(query);

		result.Value.Page.Should().Be(3);
		result.Value.PageSize.Should().Be(10);
		result.Value.TotalCount.Should().Be(50);
	}

	[Fact]
	public async Task HandleAsync_Always_FiltersOnCurrentUser()
	{
		currentUser.UserId.Returns("specific-user-id");
		SetupEmptyRepository();

		var query = CreateQuery(1, 20, RecipeSortField.Date, SortDirection.Descending);
		await handler.HandleAsync(query);

		await recipes.Received(1).GetByOwnerAsync(
			Arg.Is<OwnerIdentifier>(o => o.Value == "specific-user-id"),
			Arg.Any<PagingOptions>(),
			Arg.Any<SortingOptions<RecipeSortField>>(),
			Arg.Any<SearchTerm?>(),
			Arg.Any<RecipeFilter?>(),
			Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task HandleAsync_PageThreeWithTenPerPage_CalculatesSkipAsTwenty()
	{
		SetupEmptyRepository();

		var query = CreateQuery(3, 10, RecipeSortField.Date, SortDirection.Descending);
		await handler.HandleAsync(query);

		await recipes.Received(1).GetByOwnerAsync(
			Arg.Any<OwnerIdentifier>(),
			Arg.Is<PagingOptions>(p => p.Skip == 20 && p.PageSize.Value == 10),
			Arg.Any<SortingOptions<RecipeSortField>>(),
			Arg.Any<SearchTerm?>(),
			Arg.Any<RecipeFilter?>(),
			Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task HandleAsync_FirstPage_CalculatesSkipAsZero()
	{
		SetupEmptyRepository();

		var query = CreateQuery(1, 20, RecipeSortField.Date, SortDirection.Descending);
		await handler.HandleAsync(query);

		await recipes.Received(1).GetByOwnerAsync(
			Arg.Any<OwnerIdentifier>(),
			Arg.Is<PagingOptions>(p => p.Skip == 0 && p.PageSize.Value == 20),
			Arg.Any<SortingOptions<RecipeSortField>>(),
			Arg.Any<SearchTerm?>(),
			Arg.Any<RecipeFilter?>(),
			Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task HandleAsync_SortByNameAscending_PassesSortToRepository()
	{
		SetupEmptyRepository();

		var query = CreateQuery(1, 20, RecipeSortField.Name, SortDirection.Ascending);
		await handler.HandleAsync(query);

		await recipes.Received(1).GetByOwnerAsync(
			Arg.Any<OwnerIdentifier>(),
			Arg.Any<PagingOptions>(),
			Arg.Is<SortingOptions<RecipeSortField>>(s => s.SortBy == RecipeSortField.Name && s.Direction == SortDirection.Ascending),
			Arg.Any<SearchTerm?>(),
			Arg.Any<RecipeFilter?>(),
			Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task HandleAsync_SortByDateDescending_PassesSortToRepository()
	{
		SetupEmptyRepository();

		var query = CreateQuery(1, 20, RecipeSortField.Date, SortDirection.Descending);
		await handler.HandleAsync(query);

		await recipes.Received(1).GetByOwnerAsync(
			Arg.Any<OwnerIdentifier>(),
			Arg.Any<PagingOptions>(),
			Arg.Is<SortingOptions<RecipeSortField>>(s => s.SortBy == RecipeSortField.Date && s.Direction == SortDirection.Descending),
			Arg.Any<SearchTerm?>(),
			Arg.Any<RecipeFilter?>(),
			Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task HandleAsync_WithCancellationToken_ForwardsToRepository()
	{
		SetupEmptyRepository();
		var cts = new CancellationTokenSource();

		var query = CreateQuery(1, 20, RecipeSortField.Date, SortDirection.Descending);
		await handler.HandleAsync(query, cts.Token);

		await recipes.Received(1).GetByOwnerAsync(
			Arg.Any<OwnerIdentifier>(),
			Arg.Any<PagingOptions>(),
			Arg.Any<SortingOptions<RecipeSortField>>(),
			Arg.Any<SearchTerm?>(),
			Arg.Any<RecipeFilter?>(),
			cts.Token);
	}

	[Fact]
	public async Task HandleAsync_WithSearchTerm_ForwardsSearchToRepository()
	{
		SetupEmptyRepository();

		var search = SearchTerm.From("pasta");
		var query = CreateQuery(1, 20, RecipeSortField.Date, SortDirection.Descending, search);
		await handler.HandleAsync(query);

		await recipes.Received(1).GetByOwnerAsync(
			Arg.Any<OwnerIdentifier>(),
			Arg.Any<PagingOptions>(),
			Arg.Any<SortingOptions<RecipeSortField>>(),
			Arg.Is<SearchTerm?>(s => s != null && s.Value == "pasta"),
			Arg.Any<RecipeFilter?>(),
			Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task HandleAsync_WithoutSearchTerm_PassesNullToRepository()
	{
		SetupEmptyRepository();

		var query = CreateQuery(1, 20, RecipeSortField.Date, SortDirection.Descending);
		await handler.HandleAsync(query);

		await recipes.Received(1).GetByOwnerAsync(
			Arg.Any<OwnerIdentifier>(),
			Arg.Any<PagingOptions>(),
			Arg.Any<SortingOptions<RecipeSortField>>(),
			null,
			Arg.Any<RecipeFilter?>(),
			Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task HandleAsync_WithFilter_ForwardsFilterToRepository()
	{
		SetupEmptyRepository();
		var filter = new RecipeFilter(
			Tags: [RecipeTag.From("vegan")],
			Difficulty: Difficulty.From("easy"),
			MaxPrepTime: PreparationTime.From(15),
			MaxCookTime: CookingTime.From(30));

		var query = CreateQuery(1, 20, RecipeSortField.Date, SortDirection.Descending, filter: filter);
		await handler.HandleAsync(query);

		await recipes.Received(1).GetByOwnerAsync(
			Arg.Any<OwnerIdentifier>(),
			Arg.Any<PagingOptions>(),
			Arg.Any<SortingOptions<RecipeSortField>>(),
			Arg.Any<SearchTerm?>(),
			Arg.Is<RecipeFilter?>(f => f != null && ReferenceEquals(f, filter)),
			Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task HandleAsync_WithoutFilter_PassesNullFilterToRepository()
	{
		SetupEmptyRepository();

		var query = CreateQuery(1, 20, RecipeSortField.Date, SortDirection.Descending);
		await handler.HandleAsync(query);

		await recipes.Received(1).GetByOwnerAsync(
			Arg.Any<OwnerIdentifier>(),
			Arg.Any<PagingOptions>(),
			Arg.Any<SortingOptions<RecipeSortField>>(),
			Arg.Any<SearchTerm?>(),
			(RecipeFilter?)null,
			Arg.Any<CancellationToken>());
	}

	private static GetRecipesQuery CreateQuery(
		int page,
		int pageSize,
		RecipeSortField sortBy,
		SortDirection sortDirection,
		SearchTerm? search = null,
		RecipeFilter? filter = null)
	{
		var paging = PagingOptions.Of(Page.From(page), PageSize.From(pageSize));
		var sorting = new SortingOptions<RecipeSortField>(sortBy, sortDirection);
		return new GetRecipesQuery(paging, sorting, search, filter);
	}

	private void SetupEmptyRepository()
	{
		SetupRepository([], 0);
	}

	private void SetupRepository(IReadOnlyList<Recipe> items, int totalCount)
	{
		recipes.GetByOwnerAsync(
			Arg.Any<OwnerIdentifier>(),
			Arg.Any<PagingOptions>(),
			Arg.Any<SortingOptions<RecipeSortField>>(),
			Arg.Any<SearchTerm?>(),
			Arg.Any<RecipeFilter?>(),
			Arg.Any<CancellationToken>())
			.Returns(call =>
			{
				var paging = call.Arg<PagingOptions>();
				return new PagedResult<Recipe>(items, totalCount, paging.Page.Value, paging.PageSize.Value);
			});
	}
}
