using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shopping.Application.Queries;
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
        shoppingLists.GetByOwnerAsync(Arg.Any<OwnerIdentifier>(), Arg.Any<CancellationToken>())
            .Returns(new List<ShoppingList>());

        var result = await handler.HandleAsync(new GetShoppingListsQuery());

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    [Fact]
    public async Task HandleAsync_WithLists_ReturnsUserLists()
    {
        var lists = new List<ShoppingList>
        {
            ShoppingList.Create(
                new ShoppingListTitle("List 1"),
                new OwnerIdentifier("user-123"),
                [ShoppingListItem.Create(new ItemName("Flour"), new Amount(500), new Unit("g"))]),
            ShoppingList.Create(
                new ShoppingListTitle("List 2"),
                new OwnerIdentifier("user-123"),
                [
                    ShoppingListItem.Create(new ItemName("Sugar"), new Amount(200), new Unit("g")),
                    ShoppingListItem.Create(new ItemName("Butter"), new Amount(100), new Unit("g")),
                ]),
        };

        shoppingLists.GetByOwnerAsync(Arg.Any<OwnerIdentifier>(), Arg.Any<CancellationToken>())
            .Returns(lists);

        var result = await handler.HandleAsync(new GetShoppingListsQuery());

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
        result.Value[0].Title.Should().Be("List 1");
        result.Value[0].ItemCount.Should().Be(1);
        result.Value[1].Title.Should().Be("List 2");
        result.Value[1].ItemCount.Should().Be(2);
    }
}
