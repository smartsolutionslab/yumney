using FluentAssertions;
using Microsoft.Extensions.Caching.Distributed;
using NSubstitute;
using SmartSolutionsLab.Yumney.Recipes.Domain.Recipe;
using SmartSolutionsLab.Yumney.Recipes.Infrastructure.Services;
using SmartSolutionsLab.Yumney.Shared.Events;
using SmartSolutionsLab.Yumney.Shared.Events.Contracts;
using SmartSolutionsLab.Yumney.TestBuilders.Recipes;
using Xunit;

namespace SmartSolutionsLab.Yumney.Recipes.Infrastructure.Tests.Services;

public class CachedRecipeViewTrackerTests
{
	private readonly InMemoryDistributedCache cache = new();
	private readonly IEventBus eventBus = Substitute.For<IEventBus>();

	[Fact]
	public async Task TrackAsync_FirstViewOfRecipe_PublishesIntegrationEvent()
	{
		var owner = OwnerIdentifier.From("user-1");
		var recipe = RecipeBuilder.A().OwnedBy("user-1").WithTitle("Carbonara").Build();
		var tracker = new CachedRecipeViewTracker(cache, eventBus);

		await tracker.TrackAsync(owner, recipe);

		await eventBus.Received(1).PublishAsync(
			Arg.Is<RecipeViewedIntegrationEvent>(@event =>
				@event.OwnerId == "user-1"
				&& @event.RecipeIdentifier == recipe.Id.Value
				&& @event.RecipeTitle == "Carbonara"),
			Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task TrackAsync_RepeatedViewWithinWindow_DoesNotPublishAgain()
	{
		var owner = OwnerIdentifier.From("user-1");
		var recipe = RecipeBuilder.A().OwnedBy("user-1").Build();
		var tracker = new CachedRecipeViewTracker(cache, eventBus);

		await tracker.TrackAsync(owner, recipe);
		await tracker.TrackAsync(owner, recipe);
		await tracker.TrackAsync(owner, recipe);

		await eventBus.Received(1).PublishAsync(
			Arg.Any<RecipeViewedIntegrationEvent>(),
			Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task TrackAsync_DifferentOwners_ViewingSameRecipe_BothGetTracked()
	{
		var alice = OwnerIdentifier.From("alice");
		var bob = OwnerIdentifier.From("bob");
		var recipe = RecipeBuilder.A().Build();
		var tracker = new CachedRecipeViewTracker(cache, eventBus);

		await tracker.TrackAsync(alice, recipe);
		await tracker.TrackAsync(bob, recipe);

		await eventBus.Received(1).PublishAsync(
			Arg.Is<RecipeViewedIntegrationEvent>(@event => @event.OwnerId == "alice"),
			Arg.Any<CancellationToken>());
		await eventBus.Received(1).PublishAsync(
			Arg.Is<RecipeViewedIntegrationEvent>(@event => @event.OwnerId == "bob"),
			Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task TrackAsync_DifferentRecipesForSameUser_BothGetTracked()
	{
		var owner = OwnerIdentifier.From("user-1");
		var first = RecipeBuilder.A().WithTitle("A").Build();
		var second = RecipeBuilder.A().WithTitle("B").Build();
		var tracker = new CachedRecipeViewTracker(cache, eventBus);

		await tracker.TrackAsync(owner, first);
		await tracker.TrackAsync(owner, second);

		await eventBus.Received(1).PublishAsync(
			Arg.Is<RecipeViewedIntegrationEvent>(@event => @event.RecipeIdentifier == first.Id.Value),
			Arg.Any<CancellationToken>());
		await eventBus.Received(1).PublishAsync(
			Arg.Is<RecipeViewedIntegrationEvent>(@event => @event.RecipeIdentifier == second.Id.Value),
			Arg.Any<CancellationToken>());
	}

	// Minimal in-memory IDistributedCache to avoid pulling Microsoft.Extensions.Caching.Memory
	// into the test project just for MemoryDistributedCache.
	private sealed class InMemoryDistributedCache : IDistributedCache
	{
		private readonly Dictionary<string, byte[]> store = [];

		public byte[]? Get(string key) => store.TryGetValue(key, out var value) ? value : null;

		public Task<byte[]?> GetAsync(string key, CancellationToken token = default) =>
			Task.FromResult(Get(key));

		public void Set(string key, byte[] value, DistributedCacheEntryOptions options) => store[key] = value;

		public Task SetAsync(string key, byte[] value, DistributedCacheEntryOptions options, CancellationToken token = default)
		{
			Set(key, value, options);
			return Task.CompletedTask;
		}

		public void Refresh(string key)
		{
		}

		public Task RefreshAsync(string key, CancellationToken token = default) => Task.CompletedTask;

		public void Remove(string key) => store.Remove(key);

		public Task RemoveAsync(string key, CancellationToken token = default)
		{
			Remove(key);
			return Task.CompletedTask;
		}
	}
}
