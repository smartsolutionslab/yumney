using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingLedger;
using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingLedger.Events;
using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingList;
using SmartSolutionsLab.Yumney.Shopping.Infrastructure.Persistence;
using SmartSolutionsLab.Yumney.Shopping.Infrastructure.Persistence.EventStore;
using SmartSolutionsLab.Yumney.Shopping.Infrastructure.Persistence.ReadModel;
using Xunit;

namespace SmartSolutionsLab.Yumney.Shopping.Infrastructure.Tests.Persistence.ReadModel;

public class IngredientBalanceProjectionHandlerTests
{
	private const string OwnerId = "user-1";

	[Fact]
	public async Task Bought_NewItem_CreatesRowWithBoughtTotal()
	{
		await using var context = CreateContext();
		var handler = new IngredientBalanceProjectionHandler(context);
		var domainEvent = new ShoppingItemBought(ItemName.From("Milk"), Quantity.Of(Amount.From(2), Unit.From("l")));

		await handler.HandleAsync(new ShoppingItemBoughtIntegrationEvent(OwnerId, domainEvent));

		var row = await context.Set<IngredientBalanceReadItem>().SingleAsync();
		row.OwnerId.Should().Be(OwnerId);
		row.ItemName.Should().Be("Milk");
		row.Unit.Should().Be("l");
		row.BoughtTotal.Should().Be(2);
		row.AtHome.Should().Be(2);
	}

	[Fact]
	public async Task Consumed_AfterBought_DecrementsAtHome()
	{
		await using var context = CreateContext();
		var handler = new IngredientBalanceProjectionHandler(context);
		var bought = new ShoppingItemBought(ItemName.From("Milk"), Quantity.Of(Amount.From(5), Unit.From("l")));
		var consumed = new ShoppingItemConsumed(ItemName.From("Milk"), Quantity.Of(Amount.From(1), Unit.From("l")), ItemSource.From("meal-plan"));

		await handler.HandleAsync(new ShoppingItemBoughtIntegrationEvent(OwnerId, bought));
		await handler.HandleAsync(new ShoppingItemConsumedIntegrationEvent(OwnerId, consumed));

		var row = await context.Set<IngredientBalanceReadItem>().SingleAsync();
		row.BoughtTotal.Should().Be(5);
		row.ConsumedTotal.Should().Be(1);
		row.AtHome.Should().Be(4);
	}

	[Fact]
	public async Task Removed_AfterBought_DecrementsAtHome()
	{
		await using var context = CreateContext();
		var handler = new IngredientBalanceProjectionHandler(context);
		var bought = new ShoppingItemBought(ItemName.From("Eggs"), Quantity.Of(Amount.From(12), null));
		var removed = new ShoppingItemRemoved(ItemName.From("Eggs"), Quantity.Of(Amount.From(2), null), null);

		await handler.HandleAsync(new ShoppingItemBoughtIntegrationEvent(OwnerId, bought));
		await handler.HandleAsync(new ShoppingItemRemovedIntegrationEvent(OwnerId, removed));

		var row = await context.Set<IngredientBalanceReadItem>().SingleAsync();
		row.AtHome.Should().Be(10);
	}

	[Fact]
	public async Task UndoBought_DecrementsBoughtTotal()
	{
		await using var context = CreateContext();
		var handler = new IngredientBalanceProjectionHandler(context);
		var bought = new ShoppingItemBought(ItemName.From("Bread"), Quantity.Of(Amount.From(3), null));
		var undo = new ShoppingItemUndoBought(ItemName.From("Bread"), Quantity.Of(Amount.From(1), null));

		await handler.HandleAsync(new ShoppingItemBoughtIntegrationEvent(OwnerId, bought));
		await handler.HandleAsync(new ShoppingItemUndoBoughtIntegrationEvent(OwnerId, undo));

		var row = await context.Set<IngredientBalanceReadItem>().SingleAsync();
		row.BoughtTotal.Should().Be(2);
		row.AtHome.Should().Be(2);
	}

	[Fact]
	public async Task AddedAsAtHome_IncrementsBoughtTotal()
	{
		await using var context = CreateContext();
		var handler = new IngredientBalanceProjectionHandler(context);
		var atHome = new ShoppingItemAddedAsAtHome(ItemName.From("Butter"), Quantity.Of(Amount.From(250), Unit.From("g")));

		await handler.HandleAsync(new ShoppingItemAddedAsAtHomeIntegrationEvent(OwnerId, atHome));

		var row = await context.Set<IngredientBalanceReadItem>().SingleAsync();
		row.BoughtTotal.Should().Be(250);
		row.AtHome.Should().Be(250);
	}

	[Fact]
	public async Task UndoBought_NeverNegative()
	{
		await using var context = CreateContext();
		var handler = new IngredientBalanceProjectionHandler(context);
		var undo = new ShoppingItemUndoBought(ItemName.From("Milk"), Quantity.Of(Amount.From(10), Unit.From("l")));

		await handler.HandleAsync(new ShoppingItemUndoBoughtIntegrationEvent(OwnerId, undo));

		var row = await context.Set<IngredientBalanceReadItem>().SingleAsync();
		row.BoughtTotal.Should().Be(0);
		row.AtHome.Should().Be(0);
	}

	[Fact]
	public async Task ConsumedExceedsBought_AtHomeClampedToZero()
	{
		await using var context = CreateContext();
		var handler = new IngredientBalanceProjectionHandler(context);
		var bought = new ShoppingItemBought(ItemName.From("Apple"), Quantity.Of(Amount.From(2), null));
		var consumed = new ShoppingItemConsumed(ItemName.From("Apple"), Quantity.Of(Amount.From(5), null), ItemSource.From("meal-plan"));

		await handler.HandleAsync(new ShoppingItemBoughtIntegrationEvent(OwnerId, bought));
		await handler.HandleAsync(new ShoppingItemConsumedIntegrationEvent(OwnerId, consumed));

		var row = await context.Set<IngredientBalanceReadItem>().SingleAsync();
		row.AtHome.Should().Be(0);
	}

	[Fact]
	public async Task SameNameDifferentUnits_KeepsRowsSeparate()
	{
		await using var context = CreateContext();
		var handler = new IngredientBalanceProjectionHandler(context);
		var litres = new ShoppingItemBought(ItemName.From("Milk"), Quantity.Of(Amount.From(1), Unit.From("l")));
		var millis = new ShoppingItemBought(ItemName.From("Milk"), Quantity.Of(Amount.From(500), Unit.From("ml")));

		await handler.HandleAsync(new ShoppingItemBoughtIntegrationEvent(OwnerId, litres));
		await handler.HandleAsync(new ShoppingItemBoughtIntegrationEvent(OwnerId, millis));

		var rows = await context.Set<IngredientBalanceReadItem>().ToListAsync();
		rows.Should().HaveCount(2);
	}

	private static ShoppingDbContext CreateContext()
	{
		var options = new DbContextOptionsBuilder<ShoppingDbContext>()
			.UseInMemoryDatabase($"projection-{Guid.NewGuid()}")
			.Options;
		return new ShoppingDbContext(options);
	}
}
