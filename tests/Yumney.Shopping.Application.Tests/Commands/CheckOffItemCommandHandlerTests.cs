using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shopping.Application.Commands;
using SmartSolutionsLab.Yumney.Shopping.Application.Commands.Handlers;
using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingList;
using Xunit;

namespace SmartSolutionsLab.Yumney.Shopping.Application.Tests.Commands;

public class CheckOffItemCommandHandlerTests
{
    private readonly IShoppingListRepository shoppingLists = Substitute.For<IShoppingListRepository>();
    private readonly ICurrentUser currentUser = Substitute.For<ICurrentUser>();
    private readonly ILogger<CheckOffItemCommandHandler> logger = Substitute.For<ILogger<CheckOffItemCommandHandler>>();
    private readonly CheckOffItemCommandHandler handler;

    public CheckOffItemCommandHandlerTests()
    {
        currentUser.UserId.Returns("user-123");
        handler = new CheckOffItemCommandHandler(shoppingLists, currentUser, logger);
    }

    [Fact]
    public async Task HandleAsync_ValidCheckCommand_ReturnsSuccess()
    {
        var list = ShoppingListTestData.CreateListWithItem("user-123", out var itemId);
        shoppingLists.GetByIdForUpdateAsync(list.Id, Arg.Any<CancellationToken>()).Returns(list);
        var command = new CheckOffItemCommand(list.Id, itemId, true);

        var result = await handler.HandleAsync(command);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task HandleAsync_ValidUncheckCommand_ReturnsSuccess()
    {
        var list = ShoppingListTestData.CreateListWithItem("user-123", out var itemId);
        shoppingLists.GetByIdForUpdateAsync(list.Id, Arg.Any<CancellationToken>()).Returns(list);
        var command = new CheckOffItemCommand(list.Id, itemId, false);

        var result = await handler.HandleAsync(command);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task HandleAsync_ListNotFound_ReturnsFailure()
    {
        var listId = ShoppingListIdentifier.New();
        shoppingLists.GetByIdForUpdateAsync(listId, Arg.Any<CancellationToken>()).Returns((ShoppingList?)null);
        var command = new CheckOffItemCommand(listId, ShoppingListItemIdentifier.New(), true);

        var result = await handler.HandleAsync(command);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(CheckOffItemErrors.ListNotFound);
    }

    [Fact]
    public async Task HandleAsync_DifferentOwner_ReturnsAccessDenied()
    {
        var list = ShoppingListTestData.CreateListWithItem("other-user", out var itemId);
        shoppingLists.GetByIdForUpdateAsync(list.Id, Arg.Any<CancellationToken>()).Returns(list);
        var command = new CheckOffItemCommand(list.Id, itemId, true);

        var result = await handler.HandleAsync(command);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(CheckOffItemErrors.AccessDenied);
    }

    [Fact]
    public async Task HandleAsync_ValidCommand_CallsSaveChanges()
    {
        var list = ShoppingListTestData.CreateListWithItem("user-123", out var itemId);
        shoppingLists.GetByIdForUpdateAsync(list.Id, Arg.Any<CancellationToken>()).Returns(list);
        var command = new CheckOffItemCommand(list.Id, itemId, true);

        await handler.HandleAsync(command);

        await shoppingLists.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_CheckTrue_CallsCheckOffItem()
    {
        var list = ShoppingListTestData.CreateListWithItem("user-123", out var itemId);
        shoppingLists.GetByIdForUpdateAsync(list.Id, Arg.Any<CancellationToken>()).Returns(list);
        var command = new CheckOffItemCommand(list.Id, itemId, true);

        await handler.HandleAsync(command);

        list.Items.First(i => i.Id == itemId).IsChecked.Should().BeTrue();
    }

    [Fact]
    public async Task HandleAsync_CheckFalse_CallsUncheckItem()
    {
        var list = ShoppingListTestData.CreateListWithItem("user-123", out var itemId);
        list.CheckOffItem(itemId);
        shoppingLists.GetByIdForUpdateAsync(list.Id, Arg.Any<CancellationToken>()).Returns(list);
        var command = new CheckOffItemCommand(list.Id, itemId, false);

        await handler.HandleAsync(command);

        list.Items.First(i => i.Id == itemId).IsChecked.Should().BeFalse();
    }

    [Fact]
    public async Task HandleAsync_ForwardsCancellationToken()
    {
        var cts = new CancellationTokenSource();
        var list = ShoppingListTestData.CreateListWithItem("user-123", out var itemId);
        shoppingLists.GetByIdForUpdateAsync(list.Id, Arg.Any<CancellationToken>()).Returns(list);
        var command = new CheckOffItemCommand(list.Id, itemId, true);

        await handler.HandleAsync(command, cts.Token);

        await shoppingLists.Received(1).GetByIdForUpdateAsync(list.Id, cts.Token);
        await shoppingLists.Received(1).SaveChangesAsync(cts.Token);
    }
}
