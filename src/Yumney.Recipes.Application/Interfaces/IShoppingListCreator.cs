namespace SmartSolutionsLab.Yumney.Recipes.Application.Interfaces;

/// <summary>
/// Consumer-defined contract for creating a shopping list from one or more
/// recipes. Owned by Recipes (the chat surface is the consumer) per CLAUDE.md
/// cross-module rule + ADR 0002.
/// </summary>
public interface IShoppingListCreator
{
	/// <summary>Create a shopping list named <paramref name="request"/>.Title sourced from the given recipes.</summary>
	/// <param name="request">Title plus the list of recipes (with optional servings overrides).</param>
	/// <param name="cancellationToken">Cancellation propagated to the call.</param>
	/// <returns>True on success, false if the upstream rejected the request.</returns>
	Task<bool> CreateAsync(CreateShoppingListRequest request, CancellationToken cancellationToken = default);
}

/// <summary>Consumer-flavored shape of a create-shopping-list-from-recipes request.</summary>
/// <param name="Title">List title (e.g. "This week's dinners").</param>
/// <param name="Recipes">Recipes to draw from.</param>
public sealed record CreateShoppingListRequest(string Title, IReadOnlyList<CreateShoppingListRecipe> Recipes);

/// <summary>One recipe selection inside a create-shopping-list request.</summary>
/// <param name="RecipeIdentifier">Recipe identifier resolved from a prior search/get.</param>
/// <param name="Servings">Optional override for servings; null = use recipe default.</param>
public sealed record CreateShoppingListRecipe(Guid RecipeIdentifier, int? Servings);
