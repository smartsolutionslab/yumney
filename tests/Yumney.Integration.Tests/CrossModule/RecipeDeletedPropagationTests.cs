using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SmartSolutionsLab.Yumney.Integration.Tests.Fixtures;
using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingList;
using SmartSolutionsLab.Yumney.Shopping.Infrastructure.Persistence.ReadModel;
using Xunit;

namespace SmartSolutionsLab.Yumney.Integration.Tests.CrossModule;

/// <summary>
/// End-to-end: DELETE /api/v1/recipes/{id} publishes RecipeDeletedIntegrationEvent,
/// Shopping subscribes and nulls the RecipeReference on any of the owner's lists
/// that pointed at the deleted recipe.
/// </summary>
[Collection(AspireCollection.Name)]
public class RecipeDeletedPropagationTests(AspireFixture fixture) : IAsyncLifetime
{
	private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

	public Task InitializeAsync() => CleanupAsync();

	public Task DisposeAsync() => CleanupAsync();

	[Fact]
	public async Task DeleteRecipe_WithLinkedShoppingList_NullsRecipeReference()
	{
		using var recipesClient = await fixture.CreateAuthenticatedClientAsync("recipes-api");
		using var shoppingClient = await fixture.CreateAuthenticatedClientAsync("shopping-api");

		var recipeId = await CreateRecipeAsync(recipesClient);
		var listId = await CreateShoppingListAsync(shoppingClient, recipeId);

		// Wait for the shopping projection to index the recipe reference before
		// deleting. RecipeDeletedHandler queries the projection to find lists,
		// so the projection MUST have the row indexed before the delete event
		// arrives or the handler will return zero matches and bail out.
		await WaitForAsync(async () =>
		{
			await using var ctx = await fixture.CreateShoppingReadDbContextAsync();
			var summary = await ctx.ShoppingListSummaryReadItems
				.SingleOrDefaultAsync(s => s.Id == listId);
			return summary is not null && summary.RecipeIdentifier == recipeId;
		});

		var delete = await recipesClient.DeleteAsync($"/api/v1/recipes/{recipeId}");
		delete.StatusCode.Should().Be(HttpStatusCode.NoContent);

		await WaitForAsync(async () =>
		{
			await using var ctx = await fixture.CreateShoppingReadDbContextAsync();
			var summary = await ctx.ShoppingListSummaryReadItems
				.SingleOrDefaultAsync(s => s.Id == listId);
			return summary is not null && summary.RecipeIdentifier is null;
		});
	}

	[Fact]
	public async Task DeleteRecipe_WithUnrelatedShoppingList_LeavesReferenceIntact()
	{
		using var recipesClient = await fixture.CreateAuthenticatedClientAsync("recipes-api");
		using var shoppingClient = await fixture.CreateAuthenticatedClientAsync("shopping-api");

		var recipeA = await CreateRecipeAsync(recipesClient);
		var recipeB = await CreateRecipeAsync(recipesClient);
		var linkedToB = await CreateShoppingListAsync(shoppingClient, recipeB);

		var delete = await recipesClient.DeleteAsync($"/api/v1/recipes/{recipeA}");
		delete.StatusCode.Should().Be(HttpStatusCode.NoContent);

		await Task.Delay(2000);

		await using var ctx = await fixture.CreateShoppingReadDbContextAsync();
		var summary = await ctx.ShoppingListSummaryReadItems.SingleAsync(s => s.Id == linkedToB);
		summary.RecipeIdentifier.Should().NotBeNull();
	}

	private static async Task<Guid> CreateRecipeAsync(HttpClient client)
	{
		var response = await client.PostAsJsonAsync("/api/v1/recipes", new
		{
			title = $"Recipe-{Guid.NewGuid():N}",
			ingredients = new object[] { new { name = "Water", amount = 1m, unit = "l" } },
			steps = new object[] { new { number = 1, description = "Boil." } },
		});
		response.EnsureSuccessStatusCode();
		var body = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
		return body.GetProperty("identifier").GetGuid();
	}

	private static async Task<Guid> CreateShoppingListAsync(HttpClient client, Guid recipeId)
	{
		var response = await client.PostAsJsonAsync("/api/v1/shopping-lists/", new
		{
			title = $"List-{Guid.NewGuid():N}",
			items = new[] { new { name = "Water", amount = 1m, unit = "l" } },
			recipeIdentifier = recipeId,
		});
		response.EnsureSuccessStatusCode();
		var body = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
		return body.GetProperty("identifier").GetGuid();
	}

	private static async Task WaitForAsync(Func<Task<bool>> predicate, int timeoutSeconds = 15)
	{
		var deadline = DateTime.UtcNow.AddSeconds(timeoutSeconds);
		while (DateTime.UtcNow < deadline)
		{
			if (await predicate()) return;
			await Task.Delay(250);
		}

		throw new TimeoutException($"Condition not met within {timeoutSeconds}s");
	}

	private async Task CleanupAsync()
	{
		var userId = await fixture.GetTestUserIdAsync();
		var owner = OwnerIdentifier.From(userId);
		await fixture.ResetShoppingListEventStoreAsync(owner);
		await using (var ctx = await fixture.CreateShoppingDbContextAsync())
		{
			var summaries = await ctx.Set<ShoppingListSummaryReadItem>().Where(summary => summary.OwnerId == userId).ToListAsync();
			var items = await ctx.Set<ShoppingListItemReadItem>().Where(item => item.OwnerId == userId).ToListAsync();
			ctx.RemoveRange(summaries);
			ctx.RemoveRange(items);
			await ctx.SaveChangesAsync();
		}

		await AspireFixture.CleanupAsync(
			fixture.CreateRecipesDbContextAsync,
			ctx => ctx.Recipes.Where(recipe => recipe.Owner == global::SmartSolutionsLab.Yumney.Recipes.Domain.Recipe.OwnerIdentifier.From(userId)));
	}
}
