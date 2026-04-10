using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shopping.Application.Commands;
using SmartSolutionsLab.Yumney.Shopping.Application.Commands.Handlers;
using SmartSolutionsLab.Yumney.Shopping.Application.DTOs;
using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingLedger;
using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingList;
using Xunit;

namespace SmartSolutionsLab.Yumney.Shopping.Application.Tests.Commands;

public class AddManualItemCommandHandlerTests
{
    private readonly IShoppingLedgerRepository ledgers = Substitute.For<IShoppingLedgerRepository>();
    private readonly ICurrentUser currentUser = Substitute.For<ICurrentUser>();
    private readonly ILogger<AddManualItemCommandHandler> logger = Substitute.For<ILogger<AddManualItemCommandHandler>>();
    private readonly AddManualItemCommandHandler handler;

    public AddManualItemCommandHandlerTests()
    {
        currentUser.UserId.Returns("user-123");
        handler = new AddManualItemCommandHandler(ledgers, currentUser, logger);
    }

    [Fact]
    public async Task HandleAsync_WithExplicitQuantity_UsesProvidedValues()
    {
        var existingLedger = ShoppingLedger.Create(OwnerIdentifier.From("user-123"));
        ledgers.FindByOwnerAsync(Arg.Any<OwnerIdentifier>(), Arg.Any<CancellationToken>())
            .Returns(existingLedger);

        var command = new AddManualItemCommand("Potatoes", 2, "kg");

        var result = await handler.HandleAsync(command);

        result.IsSuccess.Should().BeTrue();
        result.Value.ItemName.Should().Be("Potatoes");
        result.Value.Quantity.Should().Be(2);
        result.Value.Unit.Should().Be("kg");
    }

    [Fact]
    public async Task HandleAsync_WithoutQuantity_ResolvesDefault()
    {
        var existingLedger = ShoppingLedger.Create(OwnerIdentifier.From("user-123"));
        ledgers.FindByOwnerAsync(Arg.Any<OwnerIdentifier>(), Arg.Any<CancellationToken>())
            .Returns(existingLedger);

        var command = new AddManualItemCommand("Milk", null, null);

        var result = await handler.HandleAsync(command);

        result.IsSuccess.Should().BeTrue();
        result.Value.Quantity.Should().Be(1);
        result.Value.Unit.Should().Be("L");
    }

    [Fact]
    public async Task HandleAsync_KnownItem_ResolvesCategory()
    {
        var existingLedger = ShoppingLedger.Create(OwnerIdentifier.From("user-123"));
        ledgers.FindByOwnerAsync(Arg.Any<OwnerIdentifier>(), Arg.Any<CancellationToken>())
            .Returns(existingLedger);

        var command = new AddManualItemCommand("Chicken", 500, "g");

        var result = await handler.HandleAsync(command);

        result.IsSuccess.Should().BeTrue();
        result.Value.Category.Should().Be("meat-fish");
    }

    [Fact]
    public async Task HandleAsync_UnknownItem_CategoryDefaultsToOther()
    {
        var existingLedger = ShoppingLedger.Create(OwnerIdentifier.From("user-123"));
        ledgers.FindByOwnerAsync(Arg.Any<OwnerIdentifier>(), Arg.Any<CancellationToken>())
            .Returns(existingLedger);

        var command = new AddManualItemCommand("Toilet Paper", 1, "pc");

        var result = await handler.HandleAsync(command);

        result.IsSuccess.Should().BeTrue();
        result.Value.Category.Should().Be("household");
    }

    [Fact]
    public async Task HandleAsync_NoLedgerExists_CreatesNewLedger()
    {
        ledgers.FindByOwnerAsync(Arg.Any<OwnerIdentifier>(), Arg.Any<CancellationToken>())
            .Returns((ShoppingLedger?)null);

        var command = new AddManualItemCommand("Salt", null, null);

        var result = await handler.HandleAsync(command);

        result.IsSuccess.Should().BeTrue();
        await ledgers.Received(1).AddAsync(Arg.Any<ShoppingLedger>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_LedgerExists_SavesChanges()
    {
        var existingLedger = ShoppingLedger.Create(OwnerIdentifier.From("user-123"));
        ledgers.FindByOwnerAsync(Arg.Any<OwnerIdentifier>(), Arg.Any<CancellationToken>())
            .Returns(existingLedger);

        var command = new AddManualItemCommand("Eggs", null, null);

        await handler.HandleAsync(command);

        await ledgers.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await ledgers.DidNotReceive().AddAsync(Arg.Any<ShoppingLedger>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_SourceIsManual()
    {
        var existingLedger = ShoppingLedger.Create(OwnerIdentifier.From("user-123"));
        ledgers.FindByOwnerAsync(Arg.Any<OwnerIdentifier>(), Arg.Any<CancellationToken>())
            .Returns(existingLedger);

        var command = new AddManualItemCommand("Bread", null, null);

        var result = await handler.HandleAsync(command);

        result.Value.Source.Should().Be("manual");
    }
}
