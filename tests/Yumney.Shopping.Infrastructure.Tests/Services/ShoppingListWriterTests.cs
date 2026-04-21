using FluentAssertions;
using NSubstitute;
using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingLedger;
using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingList;
using SmartSolutionsLab.Yumney.Shopping.Infrastructure.Services;
using Xunit;

namespace SmartSolutionsLab.Yumney.Shopping.Infrastructure.Tests.Services;

public class ShoppingListWriterTests
{
	private const string OwnerId = "user-1";

	private readonly IShoppingEventStore eventStore = Substitute.For<IShoppingEventStore>();
	private readonly ShoppingListWriter writer;

	public ShoppingListWriterTests()
	{
		writer = new ShoppingListWriter(eventStore);
	}

	[Fact]
	public async Task AddItemsAsync_NoExistingLedger_CreatesNewLedgerWithRaisedEvent()
	{
		eventStore.LoadAsync(Arg.Any<OwnerIdentifier>(), Arg.Any<CancellationToken>())
			.Returns((ShoppingLedger?)null);
		ShoppingLedger? saved = null;
		await eventStore.SaveAsync(Arg.Do<ShoppingLedger>(l => saved = l), Arg.Any<CancellationToken>());

		await writer.AddItemsAsync(OwnerId, new[]
		{
			new ShoppingItemRequest("Milk", 1m, "l", "manual"),
		});

		saved.Should().NotBeNull();
		saved!.OwnerId.Value.Should().Be(OwnerId);
		saved.UncommittedEvents.Should().HaveCount(1);
	}

	[Fact]
	public async Task AddItemsAsync_ExistingLedger_AppendsToLoadedLedger()
	{
		var existing = ShoppingLedger.Create(OwnerIdentifier.From(OwnerId));
		eventStore.LoadAsync(Arg.Any<OwnerIdentifier>(), Arg.Any<CancellationToken>()).Returns(existing);

		await writer.AddItemsAsync(OwnerId, new[]
		{
			new ShoppingItemRequest("Bread", 2m, null, "recipe"),
			new ShoppingItemRequest("Butter", 250m, "g", "recipe"),
		});

		await eventStore.Received(1).SaveAsync(existing, Arg.Any<CancellationToken>());
		existing.UncommittedEvents.Should().HaveCount(2);
	}

	[Fact]
	public async Task AddItemsAsync_WithNullUnit_PassesNullThroughToLedger()
	{
		var existing = ShoppingLedger.Create(OwnerIdentifier.From(OwnerId));
		eventStore.LoadAsync(Arg.Any<OwnerIdentifier>(), Arg.Any<CancellationToken>()).Returns(existing);

		await writer.AddItemsAsync(OwnerId, new[]
		{
			new ShoppingItemRequest("Eggs", 6m, null, "manual"),
		});

		var state = existing.Items.Values.Single();
		state.Unit.Should().BeNull();
	}

	[Fact]
	public async Task AddItemsAsync_EmptyInput_StillCallsSaveAsync()
	{
		eventStore.LoadAsync(Arg.Any<OwnerIdentifier>(), Arg.Any<CancellationToken>())
			.Returns((ShoppingLedger?)null);

		await writer.AddItemsAsync(OwnerId, Array.Empty<ShoppingItemRequest>());

		await eventStore.Received(1).SaveAsync(Arg.Any<ShoppingLedger>(), Arg.Any<CancellationToken>());
	}
}
