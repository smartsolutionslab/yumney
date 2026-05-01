using SmartSolutionsLab.Yumney.MealPlan.Application.DTOs;
using SmartSolutionsLab.Yumney.MealPlan.Application.Interfaces;
using SmartSolutionsLab.Yumney.MealPlan.Domain.WeeklyPlan;
using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shared.CQRS;

namespace SmartSolutionsLab.Yumney.MealPlan.Application.Commands.Handlers;

public sealed class GenerateShoppingListCommandHandler(
	IMealPlanReadModelRepository readModel,
	IRecipeIngredientLookup recipeIngredients,
	IStaplesProvider staplesProvider,
	IShoppingListWriter shoppingListWriter,
	ICurrentUser currentUser) : ICommandHandler<GenerateShoppingListCommand, Result<GenerateShoppingListResultDto>>
{
	public async Task<Result<GenerateShoppingListResultDto>> HandleAsync(GenerateShoppingListCommand command, CancellationToken cancellationToken = default)
	{
		var week = command.Week;
		var owner = currentUser.AsOwner();

		var planned = await readModel.GetPlannedRecipesAsync(owner, week, cancellationToken);
		if (planned.Recipes.Count == 0) return GenerateShoppingListErrors.NoRecipes;

		var merged = new Dictionary<string, MergedItem>(StringComparer.OrdinalIgnoreCase);

		foreach (var recipe in planned.Recipes)
		{
			var ingredients = await recipeIngredients.LookupAsync(SlotRecipeIdentifier.From(recipe.RecipeIdentifier), cancellationToken);
			var recipeServings = (ingredients.Count > 0 ? ingredients[0].RecipeServings : null) ?? recipe.Servings;
			var scaleFactor = recipeServings > 0 ? (decimal)recipe.Servings / recipeServings : 1m;

			foreach (var ingredient in ingredients)
			{
				var scaledAmount = ingredient.Amount.HasValue
					? Math.Round(ingredient.Amount.Value * scaleFactor, 2)
					: 0m;

				var key = $"{ingredient.Name.ToLowerInvariant()}|{ingredient.Unit ?? string.Empty}";
				if (merged.TryGetValue(key, out var existing))
				{
					existing.Quantity += scaledAmount;
				}
				else
				{
					merged[key] = new MergedItem(ingredient.Name, scaledAmount, ingredient.Unit);
				}
			}
		}

		(int staplesSkipped, List<ShoppingItemRequest> itemsToAdd) = await FilterOutStaplesAsync(owner, merged, cancellationToken);

		return new GenerateShoppingListResultDto(itemsToAdd.Count, staplesSkipped);
	}

	private async Task<(int StaplesSkipped, List<ShoppingItemRequest> ItemsToAdd)> FilterOutStaplesAsync(
		OwnerIdentifier owner,
		Dictionary<string, MergedItem> merged,
		CancellationToken cancellationToken = default)
	{
		var staples = await staplesProvider.GetStapleNamesAsync(owner, cancellationToken);
		var staplesSkipped = 0;
		var itemsToAdd = new List<ShoppingItemRequest>();

		foreach (var (name, quantity, unit) in merged.Values)
		{
			if (staples.Contains(name.ToLowerInvariant()))
			{
				staplesSkipped++;
				continue;
			}

			itemsToAdd.Add(new ShoppingItemRequest(name, quantity, unit, "meal-plan"));
		}

		if (itemsToAdd.Count > 0)
		{
			await shoppingListWriter.AddItemsAsync(owner, itemsToAdd, cancellationToken);
		}

		return (staplesSkipped, itemsToAdd);
	}

	private sealed record MergedItem(string Name, decimal Quantity, string? Unit)
	{
		public decimal Quantity { get; set; } = Quantity;
	}
}
