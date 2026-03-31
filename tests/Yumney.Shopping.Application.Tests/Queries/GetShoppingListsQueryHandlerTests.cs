using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shopping.Application.Queries;
using SmartSolutionsLab.Yumney.Shopping.Application.Queries.Handlers;
using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingList;
using Xunit;

namespace SmartSolutionsLab.Yumney.Shopping.Application.Tests.Queries;

public class GetShoppingListsQueryHandlerTests
{
    private readonly IShoppingListRepository shoppingLists = Substitute.For<IShoppingListRepository>();
    private readonly ICurrentUser currentUser = Substitute.For<ICurrentUser>();
    private readonly ILogger<GetShoppingListsQueryHandler> logger = Substitute.For<ILogger<GetShoppingListsQueryHandler>>();
    private readonly GetShoppingListsQueryHandler handler;

    public GetShoppingListsQueryHandlerTests()
    {
        currentUser.UserId.Returns("user-123");
        handler = new GetShoppingListsQueryHandler(shoppingLists, currentUser, logger);
    }

    [Fact]
    public async Task HandleAsync_EmptyList_ReturnsEmptyResult()
    {
        SetupRepository([], 0);

        var query = CreateQuery(1, 20, ShoppingListSortField.Date, SortDirection.Descending);
        var result = await handler.HandleAsync(query);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().BeEmpty();
        result.Value.TotalCount.Should().Be(0);
    }

    [Fact]
    public async Task HandleAsync_WithLists_ReturnsUserLists()
    {
        List<ShoppingList> lists =
        [
            ShoppingList.Create(
                ShoppingListTitle.From("List 1"),
                OwnerIdentifier.From("user-123"),
                [ShoppingListItem.Create(ItemName.From("Flour"), Amount.From(500), Unit.From("g"))]),
            ShoppingList.Create(
                ShoppingListTitle.From("List 2"),
                OwnerIdentifier.From("user-123"),
                [
                    ShoppingListItem.Create(ItemName.From("Sugar"), Amount.From(200), Unit.From("g")),
                    ShoppingListItem.Create(ItemName.From("Butter"), Amount.From(100), Unit.From("g")),
                ]),
        ];

        SetupRepository(lists, 2);

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
        SetupRepository([], 15);

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
        SetupRepository([], 0);

        var query = CreateQuery(1, 20, ShoppingListSortField.Date, SortDirection.Descending);
        await handler.HandleAsync(query);

        await shoppingLists.Received(1).GetByOwnerAsync(
            Arg.Is<OwnerIdentifier>(o => o.Value == "specific-user-id"),
            Arg.Any<PagingOptions>(),
            Arg.Any<SortingOptions<ShoppingListSortField>>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_SortByTitleAscending_PassesSortToRepository()
    {
        SetupRepository([], 0);

        var query = CreateQuery(1, 20, ShoppingListSortField.Title, SortDirection.Ascending);
        await handler.HandleAsync(query);

        await shoppingLists.Received(1).GetByOwnerAsync(
            Arg.Any<OwnerIdentifier>(),
            Arg.Any<PagingOptions>(),
            Arg.Is<SortingOptions<ShoppingListSortField>>(s =>
                s.SortBy == ShoppingListSortField.Title && s.Direction == SortDirection.Ascending),
            Arg.Any<CancellationToken>());
    }

    private static GetShoppingListsQuery CreateQuery(
        int page, int pageSize, ShoppingListSortField sortBy, SortDirection sortDirection)
    {
        var paging = PagingOptions.Of(Page.From(page), PageSize.From(pageSize));
        var sorting = new SortingOptions<ShoppingListSortField>(sortBy, sortDirection);
        return new GetShoppingListsQuery(paging, sorting);
    }

    private void SetupRepository(IReadOnlyList<ShoppingList> items, int totalCount)
    {
        shoppingLists.GetByOwnerAsync(
            Arg.Any<OwnerIdentifier>(),
            Arg.Any<PagingOptions>(),
            Arg.Any<SortingOptions<ShoppingListSortField>>(),
            Arg.Any<CancellationToken>())
            .Returns((items, totalCount));
    }
}
