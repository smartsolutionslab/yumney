using SmartSolutionsLab.Yumney.Recipes.Domain.Recipe;
using SmartSolutionsLab.Yumney.Recipes.Domain.RecipeFavorite;
using Wolverine.EntityFrameworkCore;

namespace SmartSolutionsLab.Yumney.Recipes.Infrastructure.Persistence;

public sealed class RecipesUnitOfWork(
	RecipesDbContext context,
	IDbContextOutbox<RecipesDbContext> outbox) : IRecipesUnitOfWork
{
	public IRecipeRepository Recipes => field ??= new RecipeRepository(context);

	public IRecipeFavoriteRepository Favorites => field ??= new RecipeFavoriteRepository(context);

	// Save first so Wolverine's EF interceptor captures any messages staged via
	// IDbContextOutbox<RecipesDbContext>.PublishAsync into the outbox table inside
	// the same Postgres transaction. FlushOutgoingMessagesAsync then nudges the
	// Wolverine relay to deliver immediately — without the flush the rows sit
	// waiting on the polling relay (~5s+) and time-sensitive cross-module flows
	// like RecipeDeletedIntegrationEvent → ShoppingList projection lag noticeably.
	// A flush failure leaves the staged rows in the outbox for retry by the
	// background relay, so the at-least-once guarantee is preserved.
	public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
	{
		var rowCount = await context.SaveChangesAsync(cancellationToken);
		await outbox.FlushOutgoingMessagesAsync();
		return rowCount;
	}
}
