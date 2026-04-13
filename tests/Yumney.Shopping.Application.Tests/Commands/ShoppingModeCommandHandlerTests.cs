using FluentAssertions;
using NSubstitute;
using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shopping.Application.Commands;
using SmartSolutionsLab.Yumney.Shopping.Application.Commands.Handlers;
using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingLedger;
using SmartSolutionsLab.Yumney.Testing;
using Xunit;

namespace SmartSolutionsLab.Yumney.Shopping.Application.Tests.Commands;

public class ShoppingModeCommandHandlerTests
{
    private readonly IShoppingEventStore eventStore = Substitute.For<IShoppingEventStore>();
    private readonly ICurrentUser currentUser = Substitute.For<ICurrentUser>();

    public ShoppingModeCommandHandlerTests()
    {
        currentUser.UserId.Returns(TestSamples.UserId);
    }

    [Fact]
    public async Task StartShoppingMode_WithExistingLedger_SavesEvent()
    {
        var handler = new StartShoppingModeCommandHandler(eventStore, currentUser);
        var ledger = ShoppingLedger.Create(TestSamples.UserId);
        eventStore.LoadAsync(TestSamples.UserId, Arg.Any<CancellationToken>()).Returns(ledger);

        var result = await handler.HandleAsync(new StartShoppingModeCommand());

        result.IsSuccess.Should().BeTrue();
        await eventStore.Received(1).SaveAsync(Arg.Any<ShoppingLedger>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task StartShoppingMode_NoLedgerExists_CreatesAndSaves()
    {
        var handler = new StartShoppingModeCommandHandler(eventStore, currentUser);
        eventStore.LoadAsync(TestSamples.UserId, Arg.Any<CancellationToken>()).Returns((ShoppingLedger?)null);

        var result = await handler.HandleAsync(new StartShoppingModeCommand());

        result.IsSuccess.Should().BeTrue();
        await eventStore.Received(1).SaveAsync(Arg.Any<ShoppingLedger>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task EndShoppingMode_WithExistingLedger_SavesEvent()
    {
        var handler = new EndShoppingModeCommandHandler(eventStore, currentUser);
        var ledger = ShoppingLedger.Create(TestSamples.UserId);
        ledger.StartShoppingMode();
        ledger.MarkCommitted();
        eventStore.LoadAsync(TestSamples.UserId, Arg.Any<CancellationToken>()).Returns(ledger);

        var result = await handler.HandleAsync(new EndShoppingModeCommand(true));

        result.IsSuccess.Should().BeTrue();
        await eventStore.Received(1).SaveAsync(Arg.Any<ShoppingLedger>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task EndShoppingMode_NoLedger_ReturnsSuccess()
    {
        var handler = new EndShoppingModeCommandHandler(eventStore, currentUser);
        eventStore.LoadAsync(TestSamples.UserId, Arg.Any<CancellationToken>()).Returns((ShoppingLedger?)null);

        var result = await handler.HandleAsync(new EndShoppingModeCommand(false));

        result.IsSuccess.Should().BeTrue();
        await eventStore.DidNotReceive().SaveAsync(Arg.Any<ShoppingLedger>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task EndShoppingMode_PassesAcceptFlag()
    {
        var handler = new EndShoppingModeCommandHandler(eventStore, currentUser);
        var ledger = ShoppingLedger.Create(TestSamples.UserId);
        ledger.StartShoppingMode();
        ledger.MarkCommitted();
        eventStore.LoadAsync(TestSamples.UserId, Arg.Any<CancellationToken>()).Returns(ledger);

        await handler.HandleAsync(new EndShoppingModeCommand(AcceptPendingChanges: true));

        ledger.IsInShoppingMode.Should().BeFalse();
    }
}
