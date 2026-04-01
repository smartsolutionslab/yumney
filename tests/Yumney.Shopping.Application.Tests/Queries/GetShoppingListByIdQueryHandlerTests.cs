using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shopping.Application.Queries;
using SmartSolutionsLab.Yumney.Shopping.Application.Queries.Handlers;
using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingList;
using Xunit;

namespace SmartSolutionsLab.Yumney.Shopping.Application.Tests.Queries;

public class GetShoppingListByIdQueryHandlerTests
{
    private readonly IShoppingListRepository shoppingLists = Substitute.For<IShoppingListRepository>();
    private readonly ICurrentUser currentUser = Substitute.For<ICurrentUser>();
    private readonly ILogger<GetShoppingListByIdQueryHandler> logger = Substitute.For<ILogger<GetShoppingListByIdQueryHandler>>();
    private readonly GetShoppingListByIdQueryHandler handler;

    public GetShoppingListByIdQueryHandlerTests()
    {
        currentUser.UserId.Returns("user-123");
        handler = new GetShoppingListByIdQueryHandler(shoppingLists, currentUser, logger);
    }

    [Fact]
    public async Task HandleAsync_ExistingList_ReturnsSuccess()
    {
        var shoppingList = CreateShoppingList();
        shoppingLists.GetByIdAsync(Arg.Any<ShoppingListIdentifier>(), Arg.Any<CancellationToken>())
            .Returns(shoppingList);

        var query = new GetShoppingListByIdQuery(shoppingList.Id);

        var result = await handler.HandleAsync(query);

        result.IsSuccess.Should().BeTrue();
        result.Value.Title.Should().Be("Test List");
    }

    [Fact]
    public async Task HandleAsync_NotFound_ReturnsFailure()
    {
        shoppingLists.GetByIdAsync(Arg.Any<ShoppingListIdentifier>(), Arg.Any<CancellationToken>())
            .Returns((ShoppingList?)null);

        var query = new GetShoppingListByIdQuery(ShoppingListIdentifier.New());

        var result = await handler.HandleAsync(query);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(GetShoppingListByIdErrors.NotFound);
    }

    [Fact]
    public async Task HandleAsync_DifferentOwner_ReturnsAccessDenied()
    {
        currentUser.UserId.Returns("different-user");
        var shoppingList = CreateShoppingList();
        shoppingLists.GetByIdAsync(Arg.Any<ShoppingListIdentifier>(), Arg.Any<CancellationToken>())
            .Returns(shoppingList);

        var query = new GetShoppingListByIdQuery(shoppingList.Id);

        var result = await handler.HandleAsync(query);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(GetShoppingListByIdErrors.AccessDenied);
    }

    private static ShoppingList CreateShoppingList()
    {
        return ShoppingList.Create(
            ShoppingListTitle.From("Test List"),
            OwnerIdentifier.From("user-123"),
            [ShoppingListItem.Create(ItemName.From("Flour"), Amount.From(500), Unit.From("g"))]);
    }
}
