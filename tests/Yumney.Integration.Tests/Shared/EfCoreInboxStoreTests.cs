using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SmartSolutionsLab.Yumney.Integration.Tests.Fixtures;
using SmartSolutionsLab.Yumney.Shared.Events;
using SmartSolutionsLab.Yumney.Shared.Persistence;
using SmartSolutionsLab.Yumney.Shopping.Infrastructure.Persistence;
using Xunit;

namespace SmartSolutionsLab.Yumney.Integration.Tests.Shared;

/// <summary>
/// Integration tests for <see cref="EfCoreInboxStore{TContext}"/> using the
/// Shopping module's DbContext (the first module to activate the inbox
/// per #280). Real PostgreSQL is required so the unique constraint on
/// (MessageId, ConsumerName) fires for the duplicate-insert path.
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
				.FirstOrDefaultAsync(m => m.MessageId == messageId && m.ConsumerName == consumerName);
			if (row is not null)
			{
				context.InboxMessages.Remove(row);
			}
		}

		await context.SaveChangesAsync();
	}

	[Fact]
	public async Task TryMarkProcessedAsync_NewPair_ReturnsTrueAndPersists()
	{
		var messageId = Guid.NewGuid();
		var consumerName = $"consumer-{Guid.NewGuid():N}";
		recorded.Add((messageId, consumerName));

		await using var context = await fixture.CreateShoppingDbContextAsync();
		var store = new EfCoreInboxStore<ShoppingDbContext>(context);

		var result = await store.TryMarkProcessedAsync(messageId, consumerName);

		result.Should().BeTrue();
		await using var verify = await fixture.CreateShoppingDbContextAsync();
		(await verify.InboxMessages.AnyAsync(m => m.MessageId == messageId && m.ConsumerName == consumerName))
			.Should().BeTrue();
	}

	[Fact]
	public async Task TryMarkProcessedAsync_DuplicatePair_ReturnsFalse()
	{
		var messageId = Guid.NewGuid();
		var consumerName = $"consumer-{Guid.NewGuid():N}";
		recorded.Add((messageId, consumerName));

		await using (var firstContext = await fixture.CreateShoppingDbContextAsync())
		{
			var first = new EfCoreInboxStore<ShoppingDbContext>(firstContext);
			(await first.TryMarkProcessedAsync(messageId, consumerName)).Should().BeTrue();
		}

		await using var secondContext = await fixture.CreateShoppingDbContextAsync();
		var second = new EfCoreInboxStore<ShoppingDbContext>(secondContext);
		var duplicateResult = await second.TryMarkProcessedAsync(messageId, consumerName);

		duplicateResult.Should().BeFalse();
	}

	[Fact]
	public async Task TryMarkProcessedAsync_DuplicatePair_DetachesFailedEntity()
	{
		var messageId = Guid.NewGuid();
		var consumerName = $"consumer-{Guid.NewGuid():N}";
		recorded.Add((messageId, consumerName));

		await using (var first = await fixture.CreateShoppingDbContextAsync())
		{
			await new EfCoreInboxStore<ShoppingDbContext>(first).TryMarkProcessedAsync(messageId, consumerName);
		}

		await using var context = await fixture.CreateShoppingDbContextAsync();
		await new EfCoreInboxStore<ShoppingDbContext>(context).TryMarkProcessedAsync(messageId, consumerName);

		context.ChangeTracker.Entries<InboxMessage>().Should().BeEmpty();
	}

	[Fact]
	public async Task TryMarkProcessedAsync_SameMessageDifferentConsumer_ReturnsTrueForBoth()
	{
		var messageId = Guid.NewGuid();
		var consumerA = $"consumer-a-{Guid.NewGuid():N}";
		var consumerB = $"consumer-b-{Guid.NewGuid():N}";
		recorded.Add((messageId, consumerA));
		recorded.Add((messageId, consumerB));

		await using var context = await fixture.CreateShoppingDbContextAsync();
		var store = new EfCoreInboxStore<ShoppingDbContext>(context);

		(await store.TryMarkProcessedAsync(messageId, consumerA)).Should().BeTrue();
		(await store.TryMarkProcessedAsync(messageId, consumerB)).Should().BeTrue();
	}
}
