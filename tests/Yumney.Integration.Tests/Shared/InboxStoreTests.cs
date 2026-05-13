using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SmartSolutionsLab.Yumney.Integration.Tests.Fixtures;
using SmartSolutionsLab.Yumney.Shared.Events;
using SmartSolutionsLab.Yumney.Shared.Persistence;
using SmartSolutionsLab.Yumney.Shopping.Infrastructure.Persistence;
using Xunit;

namespace SmartSolutionsLab.Yumney.Integration.Tests.Shared;

/// <summary>
/// Integration tests for <see cref="InboxStore{TContext}"/> using the
/// Shopping module's DbContext. Exercises the delegate-based contract:
/// successful handler runs commit the inbox row, throwing handlers roll the
/// row back, duplicate pre-checks short-circuit before invoking the handler,
/// and concurrent peers are surfaced as <see cref="InboxOutcome.DuplicateRace"/>.
/// </summary>
[Collection(AspireCollection.Name)]
public class InboxStoreTests(AspireFixture fixture) : IAsyncLifetime
{
	private readonly List<(Guid MessageId, string ConsumerName)> recorded = [];

	public Task InitializeAsync() => Task.CompletedTask;

	public async Task DisposeAsync()
	{
		if (recorded.Count == 0) return;
		await using var context = await fixture.CreateShoppingDbContextAsync();
		foreach (var (messageId, consumerName) in recorded)
		{
			var row = await context.InboxMessages
				.FirstOrDefaultAsync(m => m.MessageId == messageId && m.ConsumerName == consumerName);
			if (row is not null)
			{
				context.InboxMessages.Remove(row);
			}
		}

		await context.SaveChangesAsync();
	}

	[Fact]
	public async Task TryProcessAsync_NewPair_InvokesHandlerAndPersistsRow()
	{
		var messageId = Guid.NewGuid();
		var consumerName = $"consumer-{Guid.NewGuid():N}";
		recorded.Add((messageId, consumerName));
		var handlerCalls = 0;

		await using (var context = await fixture.CreateShoppingDbContextAsync())
		{
			var store = new InboxStore<ShoppingDbContext>(context);
			var outcome = await store.TryProcessAsync(messageId, consumerName, _ =>
			{
				handlerCalls++;
				return Task.CompletedTask;
			});

			outcome.Should().Be(InboxOutcome.Processed);
		}

		handlerCalls.Should().Be(1);

		await using var verify = await fixture.CreateShoppingDbContextAsync();
		(await verify.InboxMessages.AnyAsync(m => m.MessageId == messageId && m.ConsumerName == consumerName))
			.Should().BeTrue();
	}

	[Fact]
	public async Task TryProcessAsync_HandlerThrows_RollsBackRowAndPropagates()
	{
		var messageId = Guid.NewGuid();
		var consumerName = $"consumer-{Guid.NewGuid():N}";

		await using (var context = await fixture.CreateShoppingDbContextAsync())
		{
			var store = new InboxStore<ShoppingDbContext>(context);
			var act = async () => await store.TryProcessAsync(messageId, consumerName, _ =>
				throw new InvalidOperationException("boom"));

			await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("boom");
		}

		await using var verify = await fixture.CreateShoppingDbContextAsync();
		(await verify.InboxMessages.AnyAsync(m => m.MessageId == messageId && m.ConsumerName == consumerName))
			.Should().BeFalse();
	}

	[Fact]
	public async Task TryProcessAsync_DuplicatePair_ReportsAlreadyProcessedAndSkipsHandler()
	{
		var messageId = Guid.NewGuid();
		var consumerName = $"consumer-{Guid.NewGuid():N}";
		recorded.Add((messageId, consumerName));

		await using (var firstContext = await fixture.CreateShoppingDbContextAsync())
		{
			var first = new InboxStore<ShoppingDbContext>(firstContext);
			await first.TryProcessAsync(messageId, consumerName, _ => Task.CompletedTask);
		}

		var handlerCalls = 0;
		await using var secondContext = await fixture.CreateShoppingDbContextAsync();
		var second = new InboxStore<ShoppingDbContext>(secondContext);
		var outcome = await second.TryProcessAsync(messageId, consumerName, _ =>
		{
			handlerCalls++;
			return Task.CompletedTask;
		});

		outcome.Should().Be(InboxOutcome.AlreadyProcessed);
		handlerCalls.Should().Be(0);
	}

	[Fact]
	public async Task TryProcessAsync_SameMessageDifferentConsumer_BothProcessed()
	{
		var messageId = Guid.NewGuid();
		var consumerA = $"consumer-a-{Guid.NewGuid():N}";
		var consumerB = $"consumer-b-{Guid.NewGuid():N}";
		recorded.Add((messageId, consumerA));
		recorded.Add((messageId, consumerB));

		await using (var contextA = await fixture.CreateShoppingDbContextAsync())
		{
			var storeA = new InboxStore<ShoppingDbContext>(contextA);
			var outcomeA = await storeA.TryProcessAsync(messageId, consumerA, _ => Task.CompletedTask);
			outcomeA.Should().Be(InboxOutcome.Processed);
		}

		await using var contextB = await fixture.CreateShoppingDbContextAsync();
		var storeB = new InboxStore<ShoppingDbContext>(contextB);
		var outcomeB = await storeB.TryProcessAsync(messageId, consumerB, _ => Task.CompletedTask);
		outcomeB.Should().Be(InboxOutcome.Processed);
	}

	[Fact]
	public async Task TryProcessAsync_ConcurrentPeerCommitsDuringHandler_ReturnsDuplicateRace()
	{
		var messageId = Guid.NewGuid();
		var consumerName = $"consumer-{Guid.NewGuid():N}";
		recorded.Add((messageId, consumerName));

		// Set up: peer commits the (messageId, consumerName) row mid-handler
		// by sneaking a write through a separate context. The local
		// transaction's commit then hits the unique constraint on save and
		// rolls back, surfacing as DuplicateRace.
		await using var localContext = await fixture.CreateShoppingDbContextAsync();
		var store = new InboxStore<ShoppingDbContext>(localContext);

		var outcome = await store.TryProcessAsync(messageId, consumerName, async ct =>
		{
			await using var peerContext = await fixture.CreateShoppingDbContextAsync();
			peerContext.InboxMessages.Add(new InboxMessage
			{
				MessageId = messageId,
				ConsumerName = consumerName,
			});
			await peerContext.SaveChangesAsync(ct);
		});

		outcome.Should().Be(InboxOutcome.DuplicateRace);
	}
}
