using System.Net;
using FluentAssertions;
using SmartSolutionsLab.Yumney.Integration.Tests.Fixtures;
using SmartSolutionsLab.Yumney.Recipes.Domain.Recipe;
using Xunit;

namespace SmartSolutionsLab.Yumney.Integration.Tests.Recipes.Contract;

/// <summary>
/// Contract tests for DELETE /api/v1/recipes/{id}. Cross-user 404 lives in
/// CrossModule/OwnershipIsolationTests; this file covers the owner happy
/// path, the auth boundary, and the missing-id 404.
/// </summary>
[Collection(AspireCollection.Name)]
public class DeleteRecipeContractTests(AspireFixture fixture) : IAsyncLifetime
{
	public Task InitializeAsync() => CleanupAsync();

	public Task DisposeAsync() => CleanupAsync();

	[Fact]
	public async Task DeleteRecipe_OwnerDeletesOwnRecipe_Returns204AndRecipeIsGone()
	{
		var userId = await fixture.GetTestUserIdAsync();
		var recipe = RecipeFactory.Lasagne(userId);
		await fixture.SeedRecipesAsync(recipe);
		using var client = await fixture.CreateAuthenticatedClientAsync("recipes-api");

		var deleteResponse = await client.DeleteAsync($"/api/v1/recipes/{recipe.Id.Value}");
		deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

		var getResponse = await client.GetAsync($"/api/v1/recipes/{recipe.Id.Value}");
		getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
	}

	[Fact]
	public async Task DeleteRecipe_NonExistentId_Returns404()
	{
		using var client = await fixture.CreateAuthenticatedClientAsync("recipes-api");

		var response = await client.DeleteAsync($"/api/v1/recipes/{Guid.NewGuid()}");

		response.StatusCode.Should().Be(HttpStatusCode.NotFound);
	}

	[Fact]
	public async Task DeleteRecipe_Unauthenticated_Returns401()
	{
		var client = fixture.RecipesApi;

		var response = await client.DeleteAsync($"/api/v1/recipes/{Guid.NewGuid()}");

		response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
	}

	private async Task CleanupAsync()
	{
		var userId = await fixture.GetTestUserIdAsync();
		var owner = OwnerIdentifier.From(userId);
		await AspireFixture.CleanupAsync(
			fixture.CreateRecipesDbContextAsync,
			ctx => ctx.Recipes.Where(r => r.Owner == owner));
	}
}
