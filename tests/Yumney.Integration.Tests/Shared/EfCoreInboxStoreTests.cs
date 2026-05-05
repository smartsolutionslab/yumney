using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SmartSolutionsLab.Yumney.Integration.Tests.Fixtures;
using SmartSolutionsLab.Yumney.Shared.Persistence;
using SmartSolutionsLab.Yumney.Shopping.Infrastructure.Persistence;
using Xunit;

namespace SmartSolutionsLab.Yumney.Integration.Tests.Shared;

/// <summary>
/// Integration tests for <see cref="EfCoreInboxStore{TContext}"/> using the
/// Shopping module's DbContext. Exercises the <c>ProcessAsync</c> contract:
/// the handler runs exactly once per (messageId, consumerName), a thrown
/// handler rolls back the inbox row so a redelivery retries it, and a
/// duplicate-pair invocation is observed and skipped.
/// </summary>
[Collection(AspireCollection.Name)]
public class EfCoreInboxStoreTests(AspireFixture fixture) : IAsyncLifetime
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
				.FirstOrDefaultAsync(message => message.MessageId == messageId && message.ConsumerName == consumerName);
			if (row is not null)
			{
				context.InboxMessages.Remove(row);
			}
		}

		await context.SaveChangesAsync();
	}

	[Fact]
	public async Task ProcessAsync_NewPair_RunsHandlerAndPersistsRow()
	{
		var messageId = Guid.NewGuid();
		var consumerName = $"consumer-{Guid.NewGuid():N}";
		recorded.Add((messageId, consumerName));
		var handlerRan = false;

		await using (var context = await fixture.CreateShoppingDbContextAsync())
		{
			var store = new EfCoreInboxStore<ShoppingDbContext>(context);
			var ranHandler = await store.ProcessAsync(messageId, consumerName, () =>
			{
				handlerRan = true;
				return Task.CompletedTask;
			});

			ranHandler.Should().BeTrue();
		}

		handlerRan.Should().BeTrue();
		await using var verify = await fixture.CreateShoppingDbContextAsync();
		(await verify.InboxMessages.AnyAsync(message => message.MessageId == messageId && message.ConsumerName == consumerName))
			.Should().BeTrue();
	}

	[Fact]
	public async Task ProcessAsync_HandlerThrows_RollsBackRowAndRethrows()
	{
		var messageId = Guid.NewGuid();
		var consumerName = $"consumer-{Guid.NewGuid():N}";

		await using (var context = await fixture.CreateShoppingDbContextAsync())
		{
			var store = new EfCoreInboxStore<ShoppingDbContext>(context);

			var act = async () => await store.ProcessAsync(messageId, consumerName, () =>
				throw new InvalidOperationException("handler boom"));

			await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("handler boom");
		}

		await using var verify = await fixture.CreateShoppingDbContextAsync();
		(await verify.InboxMessages.AnyAsync(message => message.MessageId == messageId && message.ConsumerName == consumerName))
			.Should().BeFalse();
	}

	[Fact]
	public async Task ProcessAsync_DuplicatePair_SkipsHandlerAndReturnsFalse()
	{
		var messageId = Guid.NewGuid();
		var consumerName = $"consumer-{Guid.NewGuid():N}";
		recorded.Add((messageId, consumerName));

		await using (var firstContext = await fixture.CreateShoppingDbContextAsync())
		{
			var first = new EfCoreInboxStore<ShoppingDbContext>(firstContext);
			await first.ProcessAsync(messageId, consumerName, () => Task.CompletedTask);
		}

		var duplicateHandlerRan = false;
		await using var secondContext = await fixture.CreateShoppingDbContextAsync();
		var second = new EfCoreInboxStore<ShoppingDbContext>(secondContext);
		var ranHandler = await second.ProcessAsync(messageId, consumerName, () =>
		{
			duplicateHandlerRan = true;
			return Task.CompletedTask;
		});

		ranHandler.Should().BeFalse();
		duplicateHandlerRan.Should().BeFalse();
	}

	[Fact]
	public async Task ProcessAsync_SameMessageDifferentConsumer_RunsBothHandlers()
	{
		var messageId = Guid.NewGuid();
		var consumerA = $"consumer-a-{Guid.NewGuid():N}";
		var consumerB = $"consumer-b-{Guid.NewGuid():N}";
		recorded.Add((messageId, consumerA));
		recorded.Add((messageId, consumerB));

		await using (var contextA = await fixture.CreateShoppingDbContextAsync())
		{
			var storeA = new EfCoreInboxStore<ShoppingDbContext>(contextA);
			var ranA = await storeA.ProcessAsync(messageId, consumerA, () => Task.CompletedTask);
			ranA.Should().BeTrue();
		}

		await using var contextB = await fixture.CreateShoppingDbContextAsync();
		var storeB = new EfCoreInboxStore<ShoppingDbContext>(contextB);
		var ranB = await storeB.ProcessAsync(messageId, consumerB, () => Task.CompletedTask);
		ranB.Should().BeTrue();
	}
}
