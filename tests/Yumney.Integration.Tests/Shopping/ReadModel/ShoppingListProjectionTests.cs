using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SmartSolutionsLab.Yumney.Integration.Tests.Fixtures;
using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingList;
using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingList.Events;
using SmartSolutionsLab.Yumney.Shopping.Infrastructure.Persistence.EventStore;
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
		var summaries = await ctx.Set<ShoppingListSummaryReadItem>().Where(s => s.OwnerId == owner.Value).ToListAsync();
		var items = await ctx.Set<ShoppingListItemReadItem>().Where(i => i.OwnerId == owner.Value).ToListAsync();
		ctx.RemoveRange(summaries);
		ctx.RemoveRange(items);
		await ctx.SaveChangesAsync();
	}

	[Fact]
	public async Task Created_InsertsSummaryRow()
	{
		var projection = await CreateProjectionAsync();
		var inner = new ShoppingListCreated(
			ShoppingListIdentifier.From(aggregateId),
			ShoppingListTitle.From("Weekly Groceries"),
			owner,
			RecipeReference: null,
			CreatedAt: DateTime.UtcNow);

		await projection.HandleAsync(new ShoppingListCreatedIntegrationEvent(owner.Value, aggregateId, inner));

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
		var projection = await CreateProjectionAsync();
		var itemId = ShoppingListItemIdentifier.New();
		var inner = new ListItemAdded(itemId, ItemName.From("Flour"), Quantity.Of(Amount.From(500), Unit.Gram));

		await projection.HandleAsync(new ListItemAddedIntegrationEvent(owner.Value, aggregateId, inner));

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
	public async Task ItemChecked_FlipsIsCheckedFlag()
	{
		await SeedCreatedAsync();
		var itemId = await SeedItemAsync("Sugar");
		var projection = await CreateProjectionAsync();

		await projection.HandleAsync(new ListItemCheckedIntegrationEvent(
			owner.Value, aggregateId, new ListItemChecked(ShoppingListItemIdentifier.From(itemId))));

		await using var verify = await fixture.CreateShoppingDbContextAsync();
		var item = await verify.Set<ShoppingListItemReadItem>().SingleAsync(i => i.Id == itemId);
		item.IsChecked.Should().BeTrue();
	}

	[Fact]
	public async Task AllItemsChecked_FlipsEveryItemForList()
	{
		await SeedCreatedAsync();
		var first = await SeedItemAsync("Sugar");
		var second = await SeedItemAsync("Salt");
		var projection = await CreateProjectionAsync();

		await projection.HandleAsync(new AllItemsCheckedIntegrationEvent(
			owner.Value, aggregateId, new AllItemsChecked()));

		await using var verify = await fixture.CreateShoppingDbContextAsync();
		var items = await verify.Set<ShoppingListItemReadItem>()
			.Where(i => i.ListId == aggregateId).ToListAsync();
		items.Should().HaveCount(2);
		items.Should().OnlyContain(i => i.IsChecked);
		_ = first;
		_ = second;
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
		var projection = await CreateProjectionAsync();
		await projection.HandleAsync(new ShoppingListCreatedIntegrationEvent(owner.Value, aggregateId, inner));

		await projection.HandleAsync(new RecipeReferenceClearedIntegrationEvent(
			owner.Value, aggregateId, new RecipeReferenceCleared()));

		await using var verify = await fixture.CreateShoppingDbContextAsync();
		var summary = await verify.Set<ShoppingListSummaryReadItem>().SingleAsync(s => s.Id == aggregateId);
		summary.RecipeIdentifier.Should().BeNull();
	}

	[Fact]
	public async Task Created_TwiceForSameAggregate_IsIdempotent()
	{
		var projection = await CreateProjectionAsync();
		var inner = new ShoppingListCreated(
			ShoppingListIdentifier.From(aggregateId),
			ShoppingListTitle.From("Weekly Groceries"),
			owner,
			RecipeReference: null,
			CreatedAt: DateTime.UtcNow);

		await projection.HandleAsync(new ShoppingListCreatedIntegrationEvent(owner.Value, aggregateId, inner));
		await projection.HandleAsync(new ShoppingListCreatedIntegrationEvent(owner.Value, aggregateId, inner));

		await using var verify = await fixture.CreateShoppingDbContextAsync();
		var rows = await verify.Set<ShoppingListSummaryReadItem>().Where(s => s.Id == aggregateId).CountAsync();
		rows.Should().Be(1);
	}

	private async Task<ShoppingListProjection> CreateProjectionAsync()
	{
		var ctx = await fixture.CreateShoppingDbContextAsync();
		return new ShoppingListProjection(ctx);
	}

	private async Task SeedCreatedAsync()
	{
		var projection = await CreateProjectionAsync();
		var inner = new ShoppingListCreated(
			ShoppingListIdentifier.From(aggregateId),
			ShoppingListTitle.From("Weekly Groceries"),
			owner,
			RecipeReference: null,
			CreatedAt: DateTime.UtcNow);
		await projection.HandleAsync(new ShoppingListCreatedIntegrationEvent(owner.Value, aggregateId, inner));
	}

	private async Task<Guid> SeedItemAsync(string name)
	{
		var projection = await CreateProjectionAsync();
		var itemId = ShoppingListItemIdentifier.New();
		var inner = new ListItemAdded(itemId, ItemName.From(name), Quantity.Of(Amount.From(1), Unit.Gram));
		await projection.HandleAsync(new ListItemAddedIntegrationEvent(owner.Value, aggregateId, inner));
		return itemId.Value;
	}
}
