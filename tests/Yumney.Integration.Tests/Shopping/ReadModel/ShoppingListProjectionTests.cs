using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SmartSolutionsLab.Yumney.Integration.Tests.Fixtures;
using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingList;
using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingList.Events;
using SmartSolutionsLab.Yumney.Shopping.Infrastructure.Persistence.EventStore.Events;
using SmartSolutionsLab.Yumney.Shopping.Infrastructure.Persistence.ReadModel;
using Xunit;

namespace SmartSolutionsLab.Yumney.Integration.Tests.Shopping.ReadModel;

[Collection(AspireCollection.Name)]
public class ShoppingListProjectionTests(AspireFixture fixture) : IAsyncLifetime
{
	private readonly OwnerIdentifier owner = OwnerIdentifier.From($"listproj-{Guid.NewGuid():N}");
	private readonly Guid aggregateId = Guid.CreateVersion7();

	public Task InitializeAsync() => Task.CompletedTask;

	public async Task DisposeAsync()
	{
		await using var ctx = await fixture.CreateShoppingDbContextAsync();
		var summaries = await ctx.Set<ShoppingListSummaryReadItem>().Where(summary => summary.OwnerId == owner.Value).ToListAsync();
		var items = await ctx.Set<ShoppingListItemReadItem>().Where(item => item.OwnerId == owner.Value).ToListAsync();
		ctx.RemoveRange(summaries);
		ctx.RemoveRange(items);
		await ctx.SaveChangesAsync();
	}

	[Fact]
	public async Task Created_InsertsSummaryRow()
	{
		await using (var ctx = await fixture.CreateShoppingDbContextAsync())
		{
			await new ShoppingListProjection(ctx).HandleAsync(CreatedEvent());
		}

		await using var verify = await fixture.CreateShoppingDbContextAsync();
		var summary = await verify.Set<ShoppingListSummaryReadItem>().SingleAsync(s => s.Id == aggregateId);
		summary.Title.Should().Be("Weekly Groceries");
		summary.OwnerId.Should().Be(owner.Value);
		summary.ItemCount.Should().Be(0);
		summary.RecipeIdentifier.Should().BeNull();
	}

	[Fact]
	public async Task ItemAdded_InsertsItemAndIncrementsCount()
	{
		await SeedCreatedAsync();
		var itemId = ShoppingListItemIdentifier.New();

		await using (var ctx = await fixture.CreateShoppingDbContextAsync())
		{
			await new ShoppingListProjection(ctx).HandleAsync(
				ItemAddedEvent(itemId, "Flour"));
		}

		await using var verify = await fixture.CreateShoppingDbContextAsync();
		var item = await verify.Set<ShoppingListItemReadItem>().SingleAsync(i => i.Id == itemId.Value);
		item.Name.Should().Be("Flour");
		item.QuantityAmount.Should().Be(500);
		item.QuantityUnit.Should().Be("g");
		item.IsChecked.Should().BeFalse();
		item.ListId.Should().Be(aggregateId);

		var summary = await verify.Set<ShoppingListSummaryReadItem>().SingleAsync(s => s.Id == aggregateId);
		summary.ItemCount.Should().Be(1);
	}

	[Fact]
	public async Task ItemAdded_DeliveredTwice_KeepsCountAtOne()
	{
		await SeedCreatedAsync();
		var itemId = ShoppingListItemIdentifier.New();

		await using (var ctx = await fixture.CreateShoppingDbContextAsync())
		{
			await new ShoppingListProjection(ctx).HandleAsync(ItemAddedEvent(itemId, "Flour"));
		}

		await using (var ctx = await fixture.CreateShoppingDbContextAsync())
		{
			await new ShoppingListProjection(ctx).HandleAsync(ItemAddedEvent(itemId, "Flour"));
		}

		await using var verify = await fixture.CreateShoppingDbContextAsync();
		var items = await verify.Set<ShoppingListItemReadItem>().Where(item => item.ListId == aggregateId).CountAsync();
		var summary = await verify.Set<ShoppingListSummaryReadItem>().SingleAsync(s => s.Id == aggregateId);
		items.Should().Be(1);
		summary.ItemCount.Should().Be(1);
	}

	[Fact]
	public async Task ItemChecked_FlipsIsCheckedFlag()
	{
		await SeedCreatedAsync();
		var itemId = await SeedItemAsync("Sugar");

		await using (var ctx = await fixture.CreateShoppingDbContextAsync())
		{
			await new ShoppingListProjection(ctx).HandleAsync(new ListItemCheckedModuleEvent(
				owner.Value, aggregateId, new ListItemChecked(ShoppingListItemIdentifier.From(itemId))));
		}

		await using var verify = await fixture.CreateShoppingDbContextAsync();
		var item = await verify.Set<ShoppingListItemReadItem>().SingleAsync(i => i.Id == itemId);
		item.IsChecked.Should().BeTrue();
	}

	[Fact]
	public async Task AllItemsChecked_FlipsEveryItemForList()
	{
		await SeedCreatedAsync();
		await SeedItemAsync("Sugar");
		await SeedItemAsync("Salt");

		await using (var ctx = await fixture.CreateShoppingDbContextAsync())
		{
			await new ShoppingListProjection(ctx).HandleAsync(new AllItemsCheckedModuleEvent(
				owner.Value, aggregateId, new AllItemsChecked()));
		}

		await using var verify = await fixture.CreateShoppingDbContextAsync();
		var items = await verify.Set<ShoppingListItemReadItem>()
			.Where(item => item.ListId == aggregateId).ToListAsync();
		items.Should().HaveCount(2);
		items.Should().OnlyContain(i => i.IsChecked);
	}

	[Fact]
	public async Task RecipeReferenceCleared_NullsRecipeIdentifierOnSummary()
	{
		var recipe = Guid.CreateVersion7();
		var inner = new ShoppingListCreated(
			ShoppingListIdentifier.From(aggregateId),
			ShoppingListTitle.From("Cake"),
			owner,
			RecipeReference: RecipeReference.From(recipe),
			CreatedAt: DateTime.UtcNow);

		await using (var ctx = await fixture.CreateShoppingDbContextAsync())
		{
			var projection = new ShoppingListProjection(ctx);
			await projection.HandleAsync(new ShoppingListCreatedModuleEvent(owner.Value, aggregateId, inner));
		}

		await using (var ctx = await fixture.CreateShoppingDbContextAsync())
		{
			await new ShoppingListProjection(ctx).HandleAsync(new RecipeReferenceClearedModuleEvent(
				owner.Value, aggregateId, new RecipeReferenceCleared()));
		}

		await using var verify = await fixture.CreateShoppingDbContextAsync();
		var summary = await verify.Set<ShoppingListSummaryReadItem>().SingleAsync(s => s.Id == aggregateId);
		summary.RecipeIdentifier.Should().BeNull();
	}

	[Fact]
	public async Task Created_TwiceForSameAggregate_IsIdempotent()
	{
		await using (var ctx = await fixture.CreateShoppingDbContextAsync())
		{
			await new ShoppingListProjection(ctx).HandleAsync(CreatedEvent());
		}

		await using (var ctx = await fixture.CreateShoppingDbContextAsync())
		{
			await new ShoppingListProjection(ctx).HandleAsync(CreatedEvent());
		}

		await using var verify = await fixture.CreateShoppingDbContextAsync();
		var rows = await verify.Set<ShoppingListSummaryReadItem>().Where(summary => summary.Id == aggregateId).CountAsync();
		rows.Should().Be(1);
	}

	private ShoppingListCreatedModuleEvent CreatedEvent() =>
		new(owner.Value, aggregateId, new ShoppingListCreated(
			ShoppingListIdentifier.From(aggregateId),
			ShoppingListTitle.From("Weekly Groceries"),
			owner,
			RecipeReference: null,
			CreatedAt: DateTime.UtcNow));

	private ListItemAddedModuleEvent ItemAddedEvent(ShoppingListItemIdentifier itemId, string name) =>
		new(owner.Value, aggregateId, new ListItemAdded(
			itemId,
			ItemName.From(name),
			Quantity.Of(Amount.From(500), Unit.Gram)));

	private async Task SeedCreatedAsync()
	{
		await using var ctx = await fixture.CreateShoppingDbContextAsync();
		await new ShoppingListProjection(ctx).HandleAsync(CreatedEvent());
	}

	private async Task<Guid> SeedItemAsync(string name)
	{
		var itemId = ShoppingListItemIdentifier.New();
		await using var ctx = await fixture.CreateShoppingDbContextAsync();
		await new ShoppingListProjection(ctx).HandleAsync(ItemAddedEvent(itemId, name));
		return itemId.Value;
	}
}
