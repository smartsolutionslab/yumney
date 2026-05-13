using FluentAssertions;
using NSubstitute;
using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shared.Paging;
using SmartSolutionsLab.Yumney.Shopping.Application.Interfaces;
using SmartSolutionsLab.Yumney.Shopping.Application.Queries;
using SmartSolutionsLab.Yumney.Shopping.Application.Queries.Handlers;
using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingList;
using Xunit;

namespace SmartSolutionsLab.Yumney.Shopping.Application.Tests.Queries;

public class GetShoppingListsQueryHandlerTests
{
	private readonly IShoppingListProjectionRepository projection = Substitute.For<IShoppingListProjectionRepository>();
	private readonly ICurrentUser currentUser = Substitute.For<ICurrentUser>();
	private readonly GetShoppingListsQueryHandler handler;

	public GetShoppingListsQueryHandlerTests()
	{
		currentUser.UserId.Returns("user-123");
		handler = new GetShoppingListsQueryHandler(projection, currentUser);
	}

	[Fact]
	public async Task HandleAsync_EmptyList_ReturnsEmptyResult()
	{
		SetupProjection([], 0);

		var query = CreateQuery(1, 20, ShoppingListSortField.Date, SortDirection.Descending);
		var result = await handler.HandleAsync(query);

		result.IsSuccess.Should().BeTrue();
		result.Value.Items.Should().BeEmpty();
		result.Value.TotalCount.Should().Be(0);
	}

	[Fact]
	public async Task HandleAsync_WithLists_ReturnsUserLists()
	{
		List<ShoppingListSummary> summaries =
		[
			new(ShoppingListIdentifier.New(), ShoppingListTitle.From("List 1"), ItemCount.From(1), DateTime.UtcNow),
			new(ShoppingListIdentifier.New(), ShoppingListTitle.From("List 2"), ItemCount.From(2), DateTime.UtcNow)
		];

		SetupProjection(summaries, 2);

		var query = CreateQuery(1, 20, ShoppingListSortField.Date, SortDirection.Descending);
		var result = await handler.HandleAsync(query);

		result.IsSuccess.Should().BeTrue();
		result.Value.Items.Should().HaveCount(2);
		result.Value.Items[0].Title.Should().Be("List 1");
		result.Value.Items[0].ItemCount.Should().Be(1);
		result.Value.Items[1].Title.Should().Be("List 2");
		result.Value.Items[1].ItemCount.Should().Be(2);
	}

	[Fact]
	public async Task HandleAsync_PageTwoWithFivePerPage_ReturnsPaginationMetadata()
	{
		SetupProjection([], 15);

		var query = CreateQuery(2, 5, ShoppingListSortField.Date, SortDirection.Descending);
		var result = await handler.HandleAsync(query);

		result.Value.Page.Should().Be(2);
		result.Value.PageSize.Should().Be(5);
		result.Value.TotalCount.Should().Be(15);
	}

	[Fact]
	public async Task HandleAsync_Always_FiltersOnCurrentUser()
	{
		currentUser.UserId.Returns("specific-user-id");
		SetupProjection([], 0);

		var query = CreateQuery(1, 20, ShoppingListSortField.Date, SortDirection.Descending);
		await handler.HandleAsync(query);

		await projection.Received(1).GetByOwnerAsync(
			Arg.Is<OwnerIdentifier>(o => o.Value == "specific-user-id"),
			Arg.Any<PagingOptions>(),
			Arg.Any<SortingOptions<ShoppingListSortField>>(),
			Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task HandleAsync_SortByTitleAscending_PassesSortToProjection()
	{
		SetupProjection([], 0);

		var query = CreateQuery(1, 20, ShoppingListSortField.Title, SortDirection.Ascending);
		await handler.HandleAsync(query);

		await projection.Received(1).GetByOwnerAsync(
			Arg.Any<OwnerIdentifier>(),
			Arg.Any<PagingOptions>(),
			Arg.Is<SortingOptions<ShoppingListSortField>>(s =>
				s.SortBy == ShoppingListSortField.Title && s.Direction == SortDirection.Ascending),
			Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task HandleAsync_ForwardsCancellationToken()
	{
		SetupProjection([], 0);
		var cts = new CancellationTokenSource();
		var query = CreateQuery(1, 20, ShoppingListSortField.Date, SortDirection.Descending);

		await handler.HandleAsync(query, cts.Token);

		await projection.Received(1).GetByOwnerAsync(
			Arg.Any<OwnerIdentifier>(),
			Arg.Any<PagingOptions>(),
			Arg.Any<SortingOptions<ShoppingListSortField>>(),
			cts.Token);
	}

	private static GetShoppingListsQuery CreateQuery(
		int page, int pageSize, ShoppingListSortField sortBy, SortDirection sortDirection)
	{
		var paging = PagingOptions.Of(Page.From(page), PageSize.From(pageSize));
		var sorting = new SortingOptions<ShoppingListSortField>(sortBy, sortDirection);
		return new GetShoppingListsQuery(paging, sorting);
	}

	private void SetupProjection(IReadOnlyList<ShoppingListSummary> items, int totalCount)
	{
		projection.GetByOwnerAsync(
			Arg.Any<OwnerIdentifier>(),
			Arg.Any<PagingOptions>(),
			Arg.Any<SortingOptions<ShoppingListSortField>>(),
			Arg.Any<CancellationToken>())
			.Returns((items, ItemCount.From(totalCount)));
	}
}
