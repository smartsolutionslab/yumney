using FluentAssertions;
using NSubstitute;
using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shared.Outcomes;
using SmartSolutionsLab.Yumney.Shopping.Application.Commands;
using SmartSolutionsLab.Yumney.Shopping.Application.Commands.Handlers;
using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingLedger;
using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingLedger.Events;
using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingList;
using Xunit;

namespace SmartSolutionsLab.Yumney.Shopping.Application.Tests.Commands;

public class MarkAsFrozenCommandHandlerTests
{
	private readonly IShoppingEventStore eventStore = Substitute.For<IShoppingEventStore>();
	private readonly ICurrentUser currentUser = Substitute.For<ICurrentUser>();
	private readonly MarkAsFrozenCommandHandler handler;

	public MarkAsFrozenCommandHandlerTests()
	{
		currentUser.UserId.Returns("user-123");
		handler = new MarkAsFrozenCommandHandler(eventStore, currentUser);
	}

	[Fact]
	public async Task HandleAsync_NoLedger_ReturnsSuccessWithoutSaving()
	{
		eventStore.LoadAsync(Arg.Any<OwnerIdentifier>(), Arg.Any<CancellationToken>()).Returns((ShoppingLedger?)null);

		var result = await handler.HandleAsync(new MarkAsFrozenCommand(ItemName.From("Chicken"), Unit.From("g")));

		result.IsSuccess.Should().BeTrue();
		await eventStore.DidNotReceive().SaveAsync(Arg.Any<ShoppingLedger>(), Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task HandleAsync_ExistingLedger_RaisesFrozenEventAndSaves()
	{
		var ledger = ShoppingLedger.Create(OwnerIdentifier.From("user-123"));
		ledger.AddAsAtHome(ItemName.From("Chicken"), Quantity.Of(Amount.From(500), Unit.From("g")));
		ledger.MarkCommitted();
		eventStore.LoadAsync(Arg.Any<OwnerIdentifier>(), Arg.Any<CancellationToken>()).Returns(ledger);

		var result = await handler.HandleAsync(new MarkAsFrozenCommand(ItemName.From("Chicken"), Unit.From("g")));

		result.IsSuccess.Should().BeTrue();
		ledger.UncommittedEvents.OfType<ShoppingItemMarkedAsFrozen>().Should().ContainSingle();
		await eventStore.Received(1).SaveAsync(ledger, Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task HandleAsync_NullUnit_RaisesFrozenEventWithNullUnit()
	{
		var ledger = ShoppingLedger.Create(OwnerIdentifier.From("user-123"));
		ledger.MarkCommitted();
		eventStore.LoadAsync(Arg.Any<OwnerIdentifier>(), Arg.Any<CancellationToken>()).Returns(ledger);

		await handler.HandleAsync(new MarkAsFrozenCommand(ItemName.From("Eggs"), null));

		ledger.UncommittedEvents.OfType<ShoppingItemMarkedAsFrozen>().Single().Unit.Should().BeNull();
	}
}
