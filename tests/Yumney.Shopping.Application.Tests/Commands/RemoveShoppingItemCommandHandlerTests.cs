using FluentAssertions;
using NSubstitute;
using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shopping.Application.Commands;
using SmartSolutionsLab.Yumney.Shopping.Application.Commands.Handlers;
using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingLedger;
using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingList;
using Xunit;

namespace SmartSolutionsLab.Yumney.Shopping.Application.Tests.Commands;

public class RemoveShoppingItemCommandHandlerTests
{
    private readonly IShoppingEventStore eventStore = Substitute.For<IShoppingEventStore>();
    private readonly ICurrentUser currentUser = Substitute.For<ICurrentUser>();
    private readonly RemoveShoppingItemCommandHandler handler;

    public RemoveShoppingItemCommandHandlerTests()
    {
        currentUser.UserId.Returns("user-123");
        handler = new RemoveShoppingItemCommandHandler(eventStore, currentUser);
    }

    [Fact]
    public async Task HandleAsync_ExistingLedger_RemovesAndSaves()
    {
        var ledger = ShoppingLedger.Create(OwnerIdentifier.From("user-123"));
        ledger.AddItem(ItemName.From("Eggs"), Quantity.Of(Amount.From(6), Unit.From("pc")), "manual");
        ledger.MarkCommitted();
        eventStore.LoadAsync(Arg.Any<OwnerIdentifier>(), Arg.Any<CancellationToken>()).Returns(ledger);

        var command = new RemoveShoppingItemCommand(ItemName.From("Eggs"), Quantity.Of(Amount.From(6), Unit.From("pc")), RemovalReason.From("not needed"));

        var result = await handler.HandleAsync(command);

        result.IsSuccess.Should().BeTrue();
        await eventStore.Received(1).SaveAsync(Arg.Any<ShoppingLedger>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_NoLedger_ReturnsSuccess()
    {
        eventStore.LoadAsync(Arg.Any<OwnerIdentifier>(), Arg.Any<CancellationToken>()).Returns((ShoppingLedger?)null);

        var command = new RemoveShoppingItemCommand(ItemName.From("Eggs"), null, null);

        var result = await handler.HandleAsync(command);

        result.IsSuccess.Should().BeTrue();
        await eventStore.DidNotReceive().SaveAsync(Arg.Any<ShoppingLedger>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_WithoutQuantity_UsesDefaultQuantity()
    {
        var ledger = ShoppingLedger.Create(OwnerIdentifier.From("user-123"));
        ledger.AddItem(ItemName.From("Milk"), Quantity.Of(Amount.From(2), Unit.From("L")), "manual");
        ledger.MarkCommitted();
        eventStore.LoadAsync(Arg.Any<OwnerIdentifier>(), Arg.Any<CancellationToken>()).Returns(ledger);

        var command = new RemoveShoppingItemCommand(ItemName.From("Milk"), null, null);

        var result = await handler.HandleAsync(command);

        result.IsSuccess.Should().BeTrue();
        await eventStore.Received(1).SaveAsync(Arg.Any<ShoppingLedger>(), Arg.Any<CancellationToken>());
    }
}
