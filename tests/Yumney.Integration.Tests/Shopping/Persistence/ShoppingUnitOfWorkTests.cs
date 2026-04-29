using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using SmartSolutionsLab.Yumney.Integration.Tests.Fixtures;
using SmartSolutionsLab.Yumney.Shared.Events;
using SmartSolutionsLab.Yumney.Shared.Events.CrossModule;
using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingList;
using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingList.Events;
using SmartSolutionsLab.Yumney.Shopping.Infrastructure.Persistence;
using SmartSolutionsLab.Yumney.Shopping.Infrastructure.Persistence.EventStore;
using Xunit;

namespace SmartSolutionsLab.Yumney.Integration.Tests.Shopping.Persistence;

[Collection(AspireCollection.Name)]
public class ShoppingUnitOfWorkTests(AspireFixture fixture) : IAsyncLifetime
{
	private readonly OwnerIdentifier owner = OwnerIdentifier.From($"uow-{Guid.NewGuid():N}");

	public Task InitializeAsync() => Task.CompletedTask;

	public Task DisposeAsync() => fixture.ResetShoppingListEventStoreAsync(owner);

	[Fact]
	public async Task SaveChangesAsync_PublishFails_StillMarksAggregateCommitted()
	{
		await using var writeContext = await fixture.CreateShoppingDbContextAsync();
		await using var readContext = await fixture.CreateShoppingReadDbContextAsync();

		var listEventStore = new EfCoreShoppingListEventStore(
			writeContext,
			NullLogger<EfCoreShoppingListEventStore>.Instance);
		var repo = new ShoppingListRepository(writeContext, readContext);

		var failingBus = Substitute.For<IEventBus>();
		failingBus
			.PublishAsync(Arg.Any<IIntegrationEvent>(), Arg.Any<CancellationToken>())
			.ThrowsAsyncForAnyArgs(new InvalidOperationException("projection boom"));

		var uow = new ShoppingUnitOfWork(writeContext, repo, listEventStore, failingBus);

		var list = ShoppingList.Create(
			ShoppingListTitle.From("Weekly Groceries"),
			owner,
			[ShoppingListItem.Create(ItemName.From("Flour"), Quantity.Of(Amount.From(500), Unit.Gram))]);
		await repo.AddAsync(list);

		var act = () => uow.SaveChangesAsync();

		await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("projection boom");
		list.UncommittedEvents.Should().BeEmpty(
			"MarkCommitted must run before publish so a failing handler doesn't wedge the aggregate with stale events");

		// Events must still have been persisted before the publish attempt.
		await using var verify = await fixture.CreateShoppingDbContextAsync();
		var stored = await verify.Set<ShoppingListStoredEvent>()
			.Where(e => e.AggregateId == list.Identifier.Value)
			.CountAsync();
		stored.Should().Be(2);
	}

	[Fact]
	public async Task SaveChangesAsync_NewList_PublishesCrossModuleCreatedEvent()
	{
		await using var writeContext = await fixture.CreateShoppingDbContextAsync();
		await using var readContext = await fixture.CreateShoppingReadDbContextAsync();

		var listEventStore = new EfCoreShoppingListEventStore(
			writeContext,
			NullLogger<EfCoreShoppingListEventStore>.Instance);
		var repo = new ShoppingListRepository(writeContext, readContext);
		var bus = Substitute.For<IEventBus>();

		var uow = new ShoppingUnitOfWork(writeContext, repo, listEventStore, bus);
		var list = ShoppingList.Create(
			ShoppingListTitle.From("Weekly Groceries"),
			owner,
			[ShoppingListItem.Create(ItemName.From("Flour"), Quantity.Of(Amount.From(500), Unit.Gram))]);
		await repo.AddAsync(list);

		await uow.SaveChangesAsync();

		await bus.Received(1).PublishAsync(
			Arg.Is<ShoppingListCreatedCrossModuleIntegrationEvent>(e =>
				e.OwnerId == owner.Value
				&& e.ListIdentifier == list.Identifier.Value
				&& e.Title == "Weekly Groceries"
				&& e.RecipeIdentifier == null),
			Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task SaveChangesAsync_ClearRecipeReference_PublishesCrossModuleClearedEvent()
	{
		ShoppingList list;
		await using (var seedContext = await fixture.CreateShoppingDbContextAsync())
		await using (var seedRead = await fixture.CreateShoppingReadDbContextAsync())
		{
			var seedStore = new EfCoreShoppingListEventStore(seedContext, NullLogger<EfCoreShoppingListEventStore>.Instance);
			var seedRepo = new ShoppingListRepository(seedContext, seedRead);
			var seedUow = new ShoppingUnitOfWork(seedContext, seedRepo, seedStore, Substitute.For<IEventBus>());
			list = ShoppingList.Create(
				ShoppingListTitle.From("Cake"),
				owner,
				[ShoppingListItem.Create(ItemName.From("Flour"), Quantity.Of(Amount.From(500), Unit.Gram))],
				RecipeReference.New());
			await seedRepo.AddAsync(list);
			await seedUow.SaveChangesAsync();
		}

		await using var writeContext = await fixture.CreateShoppingDbContextAsync();
		await using var readContext = await fixture.CreateShoppingReadDbContextAsync();
		var listEventStore = new EfCoreShoppingListEventStore(writeContext, NullLogger<EfCoreShoppingListEventStore>.Instance);
		var repo = new ShoppingListRepository(writeContext, readContext);
		var bus = Substitute.For<IEventBus>();
		var uow = new ShoppingUnitOfWork(writeContext, repo, listEventStore, bus);

		var loaded = await repo.GetByIdForUpdateAsync(list.Identifier);
		loaded.ClearRecipeReference();
		await uow.SaveChangesAsync();

		await bus.Received(1).PublishAsync(
			Arg.Is<ShoppingListRecipeReferenceClearedCrossModuleIntegrationEvent>(e =>
				e.OwnerId == owner.Value
				&& e.ListIdentifier == list.Identifier.Value),
			Arg.Any<CancellationToken>());
	}
}
