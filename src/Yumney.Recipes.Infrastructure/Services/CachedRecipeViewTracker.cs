using Microsoft.Extensions.Caching.Distributed;
using SmartSolutionsLab.Yumney.Recipes.Application.Interfaces;
using SmartSolutionsLab.Yumney.Recipes.Domain.Recipe;
using SmartSolutionsLab.Yumney.Shared.Events;
using SmartSolutionsLab.Yumney.Shared.Events.Contracts;

namespace SmartSolutionsLab.Yumney.Recipes.Infrastructure.Services;

/// <summary>
/// IDistributedCache-backed view tracker (US-121). Uses a 5-minute per-(user,
/// recipe) lockout so a refresh storm doesn't flood the activity log with
/// duplicate views. The cache key carries the owner so two users opening the
/// same recipe each get tracked.
/// </summary>
#pragma warning disable SA1311
public sealed class CachedRecipeViewTracker(IDistributedCache cache, IEventBus eventBus) : IRecipeViewTracker
{
	private static readonly DistributedCacheEntryOptions windowOptions = new()
	{
		AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5),
	};
#pragma warning restore SA1311

	public async Task TrackAsync(OwnerIdentifier owner, Recipe recipe, CancellationToken cancellationToken = default)
	{
		var key = $"recipe-view:{owner.Value}:{recipe.Id.Value}";
		var existing = await cache.GetStringAsync(key, cancellationToken);
		if (existing is not null) return;

		await cache.SetStringAsync(key, "1", windowOptions, cancellationToken);
		await eventBus.PublishAsync(
			new RecipeViewedIntegrationEvent(owner.Value, recipe.Id.Value, recipe.Title.Value),
			cancellationToken);
	}
}
