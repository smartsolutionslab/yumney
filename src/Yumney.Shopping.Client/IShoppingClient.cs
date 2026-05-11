using SmartSolutionsLab.Yumney.Shared.Quantities;

namespace SmartSolutionsLab.Yumney.Shopping.Client;

public interface IShoppingClient
{
	Task<ShoppingBalanceResponse?> GetBalanceAsync(CancellationToken cancellationToken = default);

	Task AddItemAsync(AddShoppingItemRequest request, CancellationToken cancellationToken = default);

	/// <summary>Fetch the user's merged active shopping list. GET /api/v1/shopping-lists/merged.</summary>
	/// <param name="includePastBought">Whether to include items that have been bought.</param>
	/// <param name="cancellationToken">Cancellation propagated to the HTTP call.</param>
	/// <returns>The merged list, or null if the upstream call fails / returns 404.</returns>
	Task<MergedShoppingListResponse?> GetMergedListAsync(bool includePastBought = false, CancellationToken cancellationToken = default);

	/// <summary>Create a shopping list from one or more recipes. POST /api/v1/shopping-lists/from-recipes.</summary>
	/// <param name="body">Title plus the list of recipes to include.</param>
	/// <param name="cancellationToken">Cancellation propagated to the HTTP call.</param>
	/// <returns>True on 2xx, false on any error.</returns>
	Task<bool> CreateListFromRecipesAsync(CreateListFromRecipesBody body, CancellationToken cancellationToken = default);

	/// <summary>Remove an item from the user's active shopping list. DELETE /api/v1/shopping-lists/items.</summary>
	/// <param name="body">Item name + optional quantity / reason.</param>
	/// <param name="cancellationToken">Cancellation propagated to the HTTP call.</param>
	/// <returns>True on 2xx, false on any error.</returns>
	Task<bool> RemoveItemAsync(RemoveShoppingItemBody body, CancellationToken cancellationToken = default);
}

public sealed record ShoppingBalanceResponse(IReadOnlyList<ShoppingBalanceItem> Items);

public sealed record ShoppingBalanceItem(string ItemName, Freshness Freshness);

public sealed record AddShoppingItemRequest(string Name, decimal Quantity, string? Unit, string Source);

/// <summary>Trimmed merged-list shape returned over the wire.</summary>
public sealed record MergedShoppingListResponse(IReadOnlyList<MergedShoppingListItemResponse> Items);

/// <summary>One merged item across all of the user's active lists.</summary>
public sealed record MergedShoppingListItemResponse(
	string ItemName,
	decimal DisplayQuantity,
	string? Unit,
	string Category,
	bool IsBought);

/// <summary>Body for create-list-from-recipes POST.</summary>
public sealed record CreateListFromRecipesBody(string Title, IReadOnlyList<CreateListRecipeBody> Recipes);

/// <summary>One recipe selection inside a create-list-from-recipes body.</summary>
public sealed record CreateListRecipeBody(Guid RecipeIdentifier, int? Servings);

/// <summary>Body for remove-item DELETE.</summary>
public sealed record RemoveShoppingItemBody(string Name, decimal? Quantity = null, string? Unit = null, string? Reason = null);
