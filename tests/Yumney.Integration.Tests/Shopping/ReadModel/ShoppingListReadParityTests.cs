using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using SmartSolutionsLab.Yumney.Integration.Tests.Fixtures;
using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shared.Events;
using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingList;
using SmartSolutionsLab.Yumney.Shopping.Infrastructure.Persistence;
using SmartSolutionsLab.Yumney.Shopping.Infrastructure.Persistence.EventStore;
using SmartSolutionsLab.Yumney.Shopping.Infrastructure.Persistence.ReadModel;
using Xunit;

namespace SmartSolutionsLab.Yumney.Integration.Tests.Shopping.ReadModel;

/// <summary>
/// Phase 4 acceptance: the projection path returns equivalent data to the
/// legacy relational path for the same input. Both paths are populated by a
/// single UoW.SaveChanges (dual-write from Phase 2 + projection from Phase 3).
/// </summary>
[Collection(AspireCollection.Name)]
public class ShoppingListReadParityTests(AspireFixture fixture) : IAsyncLifetime
{
	private static readonly PagingOptions DefaultPaging = PagingOptions.Of(Page.From(1), PageSize.From(20));
	private static readonly SortingOptions<ShoppingListSortField> ByDateDesc = new(ShoppingListSortField.Date, SortDirection.Descending);

	private readonly OwnerIdentifier owner = OwnerIdentifier.From($"parity-{Guid.NewGuid():N}");

	public Task InitializeAsync() => Task.CompletedTask;

	public async Task DisposeAsync()
	{
		await fixture.ResetShoppingListEventStoreAsync(owner);
		await using var ctx = await fixture.CreateShoppingDbContextAsync();
		var summaries = await ctx.Set<ShoppingListSummaryReadItem>().Where(s => s.OwnerId == owner.Value).ToListAsync();
		var items = await ctx.Set<ShoppingListItemReadItem>().Where(i => i.OwnerId == owner.Value).ToListAsync();
		var lists = await ctx.ShoppingLists.Where(l => l.Owner == owner).ToListAsync();
		ctx.RemoveRange(summaries);
		ctx.RemoveRange(items);
		ctx.RemoveRange(lists);
		await ctx.SaveChangesAsync();
	}

	[Fact]
	public async Task GetById_LegacyAndProjection_ReturnEquivalentData()
	{
		var listId = await SeedListAsync("Weekly Groceries", "Flour", "Sugar");

		await using var writeCtx = await fixture.CreateShoppingDbContextAsync();
		await using var readCtx = await fixture.CreateShoppingReadDbContextAsync();
		var legacy = new ShoppingListRepository(writeCtx, readCtx);
		var projection = new EfCoreShoppingListProjectionRepository(readCtx);

		var legacyResult = await legacy.GetByIdAsync(listId);
		var projectionResult = await projection.GetByIdAsync(listId);

		projectionResult.OwnerId.Should().Be(legacyResult.Owner.Value);
		projectionResult.Dto.Identifier.Should().Be(legacyResult.Identifier.Value);
		projectionResult.Dto.Title.Should().Be(legacyResult.Title.Value);
		projectionResult.Dto.RecipeReference.Should().Be(legacyResult.RecipeReference?.Value);
		projectionResult.Dto.Items.Select(i => i.Name).Should().BeEquivalentTo(legacyResult.Items.Select(i => i.Name.Value));
		projectionResult.Dto.Items.Select(i => i.IsChecked).Should().BeEquivalentTo(legacyResult.Items.Select(i => i.IsChecked));
	}

	[Fact]
	public async Task GetByOwner_LegacyAndProjection_ReturnEquivalentSummaries()
	{
		await SeedListAsync("List A", "Flour");
		await SeedListAsync("List B", "Salt", "Pepper");

		await using var writeCtx = await fixture.CreateShoppingDbContextAsync();
		await using var readCtx = await fixture.CreateShoppingReadDbContextAsync();
		var legacy = new ShoppingListRepository(writeCtx, readCtx);
		var projection = new EfCoreShoppingListProjectionRepository(readCtx);

		var (legacyItems, legacyTotal) = await legacy.GetByOwnerAsync(owner, DefaultPaging, ByDateDesc);
		var (projectionItems, projectionTotal) = await projection.GetByOwnerAsync(owner, DefaultPaging, ByDateDesc);

		projectionTotal.Value.Should().Be(legacyTotal.Value);
		projectionItems.Select(s => s.Title.Value).Should().BeEquivalentTo(legacyItems.Select(s => s.Title.Value));
		projectionItems.Select(s => s.ItemCount.Value).Should().BeEquivalentTo(legacyItems.Select(s => s.ItemCount.Value));
	}

	[Fact]
	public async Task GetById_AfterCheckOff_ProjectionReflectsCheckedState()
	{
		var listId = await SeedListAsync("Groceries", "Bread");
		await CheckOffFirstItemAsync(listId);

		await using var readCtx = await fixture.CreateShoppingReadDbContextAsync();
		var projection = new EfCoreShoppingListProjectionRepository(readCtx);
		var detail = await projection.GetByIdAsync(listId);

		detail.Dto.Items.Should().ContainSingle().Which.IsChecked.Should().BeTrue();
	}

	private async Task<ShoppingListIdentifier> SeedListAsync(string title, params string[] itemNames)
	{
		await using var writeCtx = await fixture.CreateShoppingDbContextAsync();
		await using var readCtx = await fixture.CreateShoppingReadDbContextAsync();
		var legacy = new ShoppingListRepository(writeCtx, readCtx);
		var listEventStore = new EfCoreShoppingListEventStore(writeCtx, NullLogger<EfCoreShoppingListEventStore>.Instance);
		var bus = await CreateProjectingBusAsync();
		var uow = new ShoppingUnitOfWork(writeCtx, legacy, listEventStore, bus);

		var items = itemNames
			.Select(n => ShoppingListItem.Create(ItemName.From(n), Quantity.Of(Amount.From(1), Unit.Gram)))
			.ToList();
		var list = ShoppingList.Create(ShoppingListTitle.From(title), owner, items);
		await legacy.AddAsync(list);
		await uow.SaveChangesAsync();

		return list.Identifier;
	}

	private async Task CheckOffFirstItemAsync(ShoppingListIdentifier listId)
	{
		await using var writeCtx = await fixture.CreateShoppingDbContextAsync();
		await using var readCtx = await fixture.CreateShoppingReadDbContextAsync();
		var legacy = new ShoppingListRepository(writeCtx, readCtx);
		var listEventStore = new EfCoreShoppingListEventStore(writeCtx, NullLogger<EfCoreShoppingListEventStore>.Instance);
		var bus = await CreateProjectingBusAsync();
		var uow = new ShoppingUnitOfWork(writeCtx, legacy, listEventStore, bus);

		var list = await legacy.GetByIdForUpdateAsync(listId);
		list.CheckOffItem(list.Items[0].Id);
		await uow.SaveChangesAsync();
	}

	/// <summary>
	/// Wires a tiny in-place IEventBus that hands integration events directly to a
	/// fresh ShoppingListProjection backed by its own DbContext — keeps these tests
	/// self-contained without spinning up the full DI graph.
	/// </summary>
	private async Task<IEventBus> CreateProjectingBusAsync()
	{
		var bus = Substitute.For<IEventBus>();
		bus.PublishAsync(Arg.Any<IIntegrationEvent>(), Arg.Any<CancellationToken>())
			.Returns(async ci =>
			{
				var integrationEvent = ci.Arg<IIntegrationEvent>();
				var ct = ci.Arg<CancellationToken>();
				await using var ctx = await fixture.CreateShoppingDbContextAsync();
				var projection = new ShoppingListProjection(ctx);
				switch (integrationEvent)
				{
					case ShoppingListCreatedIntegrationEvent e: await projection.HandleAsync(e, ct); break;
					case ListItemAddedIntegrationEvent e: await projection.HandleAsync(e, ct); break;
					case ListItemCheckedIntegrationEvent e: await projection.HandleAsync(e, ct); break;
					case ListItemUncheckedIntegrationEvent e: await projection.HandleAsync(e, ct); break;
					case AllItemsCheckedIntegrationEvent e: await projection.HandleAsync(e, ct); break;
					case AllItemsUncheckedIntegrationEvent e: await projection.HandleAsync(e, ct); break;
					case RecipeReferenceClearedIntegrationEvent e: await projection.HandleAsync(e, ct); break;
				}
			});
		await Task.CompletedTask;
		return bus;
	}
}
