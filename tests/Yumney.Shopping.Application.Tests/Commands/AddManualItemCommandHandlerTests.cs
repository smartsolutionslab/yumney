using FluentAssertions;
using NSubstitute;
using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shared.Outcomes;
using SmartSolutionsLab.Yumney.Shopping.Application.Commands;
using SmartSolutionsLab.Yumney.Shopping.Application.Commands.Handlers;
using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingLedger;
using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingList;
using Xunit;

namespace SmartSolutionsLab.Yumney.Shopping.Application.Tests.Commands;

public class AddManualItemCommandHandlerTests
{
	private readonly IShoppingEventStore eventStore = Substitute.For<IShoppingEventStore>();
	private readonly ICurrentUser currentUser = Substitute.For<ICurrentUser>();
	private readonly AddManualItemCommandHandler handler;

	public AddManualItemCommandHandlerTests()
	{
		currentUser.UserId.Returns("user-123");
		handler = new AddManualItemCommandHandler(eventStore, currentUser);
	}

	[Fact]
	public async Task HandleAsync_WithExplicitQuantity_UsesProvidedValues()
	{
		var existingLedger = ShoppingLedger.Create(OwnerIdentifier.From("user-123"));
		eventStore.LoadAsync(Arg.Any<OwnerIdentifier>(), Arg.Any<CancellationToken>())
			.Returns(existingLedger);

		var command = new AddManualItemCommand(ItemName.From("Potatoes"), Quantity.Of(Amount.From(2), Unit.From("kg")), ItemSource.Manual);

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
		eventStore.LoadAsync(Arg.Any<OwnerIdentifier>(), Arg.Any<CancellationToken>())
			.Returns(existingLedger);

		var command = new AddManualItemCommand(ItemName.From("Milk"), null, ItemSource.Manual);

		var result = await handler.HandleAsync(command);

		result.IsSuccess.Should().BeTrue();
		result.Value.Quantity.Should().Be(1);
		result.Value.Unit.Should().Be("L");
	}

	[Fact]
	public async Task HandleAsync_KnownItem_ResolvesCategory()
	{
		var existingLedger = ShoppingLedger.Create(OwnerIdentifier.From("user-123"));
		eventStore.LoadAsync(Arg.Any<OwnerIdentifier>(), Arg.Any<CancellationToken>())
			.Returns(existingLedger);

		var command = new AddManualItemCommand(ItemName.From("Chicken"), Quantity.Of(Amount.From(500), Unit.From("g")), ItemSource.Manual);

		var result = await handler.HandleAsync(command);

		result.IsSuccess.Should().BeTrue();
		result.Value.Category.Should().Be("meat-fish");
	}

	[Fact]
	public async Task HandleAsync_NoLedgerExists_CreatesNew()
	{
		eventStore.LoadAsync(Arg.Any<OwnerIdentifier>(), Arg.Any<CancellationToken>())
			.Returns((ShoppingLedger?)null);

		var command = new AddManualItemCommand(ItemName.From("Salt"), null, ItemSource.Manual);

		var result = await handler.HandleAsync(command);

		result.IsSuccess.Should().BeTrue();
		await eventStore.Received(1).SaveAsync(Arg.Any<ShoppingLedger>(), Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task HandleAsync_SourceIsManual()
	{
		var existingLedger = ShoppingLedger.Create(OwnerIdentifier.From("user-123"));
		eventStore.LoadAsync(Arg.Any<OwnerIdentifier>(), Arg.Any<CancellationToken>())
			.Returns(existingLedger);

		var command = new AddManualItemCommand(ItemName.From("Bread"), null, ItemSource.Manual);

		var result = await handler.HandleAsync(command);

		result.Value.Source.Should().Be("manual");
	}

	[Fact]
	public async Task HandleAsync_MealPlanSource_TagsItemAsMealPlan()
	{
		var existingLedger = ShoppingLedger.Create(OwnerIdentifier.From("user-123"));
		eventStore.LoadAsync(Arg.Any<OwnerIdentifier>(), Arg.Any<CancellationToken>())
			.Returns(existingLedger);

		var command = new AddManualItemCommand(ItemName.From("Pasta"), null, ItemSource.MealPlan);

		var result = await handler.HandleAsync(command);

		result.Value.Source.Should().Be("meal-plan");
	}
}
