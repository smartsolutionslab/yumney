using System;
using System.Threading;
using System.Threading.Tasks;
using Aspire.Hosting;
using Aspire.Hosting.Testing;
using FluentAssertions;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using SmartSolutionsLab.Yumney.Integration.Tests.Fixtures;
using SmartSolutionsLab.Yumney.Recipes.Domain.Recipe;
using SmartSolutionsLab.Yumney.Recipes.Infrastructure.Services;
using SmartSolutionsLab.Yumney.Shared.Events;
using SmartSolutionsLab.Yumney.Shared.Events.Contracts;
using Xunit;

namespace SmartSolutionsLab.Yumney.Integration.Tests.CrossModule;

/// <summary>
/// Locks in the cross-instance dedup contract for <c>CachedRecipeViewTracker</c>
/// (US-121, #577). Two trackers wired against separate <c>IDistributedCache</c>
/// instances that share the same Redis backplane must collectively publish only
/// one <c>RecipeViewedIntegrationEvent</c> for the same (owner, recipe) within
/// the 5-minute lockout window — i.e. a load-balanced fleet doesn't multiply
/// activity-log writes by replica count.
///
/// Without this test, swapping <c>IDistributedCache</c> back to the in-memory
/// implementation would silently break cross-replica dedup; consumers would
/// each maintain their own short window and the activity log would gain an
/// entry per replica per refresh.
/// </summary>
[Collection(AspireCollection.Name)]
public class CachedRecipeViewTrackerCrossInstanceTests(AspireFixture fixture)
{
	[Fact]
	public async Task TwoInstancesSharingRedisBackplane_PublishOnlyOnceForSameOwnerAndRecipe()
	{
		var connectionString = await fixture.App.GetConnectionStringAsync("redis");
		connectionString.Should().NotBeNullOrEmpty(
			"the redis resource must expose a connection string for the cross-instance dedup test");

		await using var providerA = BuildIsolatedRedisServiceProvider(connectionString!);
		await using var providerB = BuildIsolatedRedisServiceProvider(connectionString!);

		var cacheA = providerA.GetRequiredService<IDistributedCache>();
		var cacheB = providerB.GetRequiredService<IDistributedCache>();

		var busA = Substitute.For<IEventBus>();
		var busB = Substitute.For<IEventBus>();

		var trackerA = new CachedRecipeViewTracker(cacheA, busA);
		var trackerB = new CachedRecipeViewTracker(cacheB, busB);

		// Unique per-test owner so a parallel run against the same Redis can't
		// pre-poison the dedup window.
		var owner = OwnerIdentifier.From($"redis-dedup-{Guid.NewGuid():N}");
		var recipe = RecipeFactory.Create("Cross-instance dedup test", owner: owner.Value);

		await trackerA.TrackAsync(owner, recipe);
		await trackerB.TrackAsync(owner, recipe);

		// First tracker writes the dedup key + publishes; second tracker sees
		// the key on its own provider's cache (same Redis) and short-circuits.
		await busA.Received(1).PublishAsync(
			Arg.Is<RecipeViewedIntegrationEvent>(evt => evt.OwnerId == owner.Value && evt.RecipeIdentifier == recipe.Id.Value),
			Arg.Any<CancellationToken>());
		await busB.DidNotReceive().PublishAsync(
			Arg.Any<RecipeViewedIntegrationEvent>(),
			Arg.Any<CancellationToken>());
	}

	private static ServiceProvider BuildIsolatedRedisServiceProvider(string connectionString)
	{
		// Two separate providers ⇒ two separate IDistributedCache instances,
		// the deployed-replica analogue. Both still resolve to the same Redis
		// instance via the connection string, which is the property under test.
		var services = new ServiceCollection();
		services.AddStackExchangeRedisCache(options => options.Configuration = connectionString);
		return services.BuildServiceProvider();
	}
}
