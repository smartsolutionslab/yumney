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
/// Shopping module's DbContext. Exercises the scope-based atomic contract:
/// commit persists the inbox row, rollback discards it (and any handler
/// writes), and a duplicate pre-check is observed without inserting.
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
	public async Task BeginAsync_NewPair_AllowsProcessAndPersistsOnCommit()
	{
		var messageId = Guid.NewGuid();
		var consumerName = $"consumer-{Guid.NewGuid():N}";
		recorded.Add((messageId, consumerName));

		await using (var context = await fixture.CreateShoppingDbContextAsync())
		{
			var store = new EfCoreInboxStore<ShoppingDbContext>(context);
			await using var scope = await store.BeginAsync(messageId, consumerName);

			scope.ShouldProcess.Should().BeTrue();
			await scope.CommitAsync();
		}

		await using var verify = await fixture.CreateShoppingDbContextAsync();
		(await verify.InboxMessages.AnyAsync(m => m.MessageId == messageId && m.ConsumerName == consumerName))
			.Should().BeTrue();
	}

	[Fact]
	public async Task BeginAsync_RollbackBeforeCommit_DropsTheRow()
	{
		var messageId = Guid.NewGuid();
		var consumerName = $"consumer-{Guid.NewGuid():N}";

		await using (var context = await fixture.CreateShoppingDbContextAsync())
		{
			var store = new EfCoreInboxStore<ShoppingDbContext>(context);
			await using var scope = await store.BeginAsync(messageId, consumerName);

			scope.ShouldProcess.Should().BeTrue();
			await scope.RollbackAsync();
		}

		await using var verify = await fixture.CreateShoppingDbContextAsync();
		(await verify.InboxMessages.AnyAsync(m => m.MessageId == messageId && m.ConsumerName == consumerName))
			.Should().BeFalse();
	}

	[Fact]
	public async Task BeginAsync_DisposeWithoutCommit_RollsBack()
	{
		var messageId = Guid.NewGuid();
		var consumerName = $"consumer-{Guid.NewGuid():N}";

		await using (var context = await fixture.CreateShoppingDbContextAsync())
		{
			var store = new EfCoreInboxStore<ShoppingDbContext>(context);
			await using var scope = await store.BeginAsync(messageId, consumerName);

			scope.ShouldProcess.Should().BeTrue();
		}

		await using var verify = await fixture.CreateShoppingDbContextAsync();
		(await verify.InboxMessages.AnyAsync(m => m.MessageId == messageId && m.ConsumerName == consumerName))
			.Should().BeFalse();
	}

	[Fact]
	public async Task BeginAsync_DuplicatePair_ReportsAlreadyProcessed()
	{
		var messageId = Guid.NewGuid();
		var consumerName = $"consumer-{Guid.NewGuid():N}";
		recorded.Add((messageId, consumerName));

		await using (var firstContext = await fixture.CreateShoppingDbContextAsync())
		{
			var first = new EfCoreInboxStore<ShoppingDbContext>(firstContext);
			await using var scope = await first.BeginAsync(messageId, consumerName);
			await scope.CommitAsync();
		}

		await using var secondContext = await fixture.CreateShoppingDbContextAsync();
		var second = new EfCoreInboxStore<ShoppingDbContext>(secondContext);
		await using var duplicateScope = await second.BeginAsync(messageId, consumerName);

		duplicateScope.ShouldProcess.Should().BeFalse();
	}

	[Fact]
	public async Task BeginAsync_SameMessageDifferentConsumer_AllowsBoth()
	{
		var messageId = Guid.NewGuid();
		var consumerA = $"consumer-a-{Guid.NewGuid():N}";
		var consumerB = $"consumer-b-{Guid.NewGuid():N}";
		recorded.Add((messageId, consumerA));
		recorded.Add((messageId, consumerB));

		await using (var contextA = await fixture.CreateShoppingDbContextAsync())
		{
			var storeA = new EfCoreInboxStore<ShoppingDbContext>(contextA);
			await using var scopeA = await storeA.BeginAsync(messageId, consumerA);
			scopeA.ShouldProcess.Should().BeTrue();
			await scopeA.CommitAsync();
		}

		await using var contextB = await fixture.CreateShoppingDbContextAsync();
		var storeB = new EfCoreInboxStore<ShoppingDbContext>(contextB);
		await using var scopeB = await storeB.BeginAsync(messageId, consumerB);
		scopeB.ShouldProcess.Should().BeTrue();
		await scopeB.CommitAsync();
	}
}
