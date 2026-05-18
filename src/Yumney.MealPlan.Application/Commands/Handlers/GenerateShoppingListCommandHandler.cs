using SmartSolutionsLab.Yumney.MealPlan.Application.DTOs;
using SmartSolutionsLab.Yumney.MealPlan.Application.Interfaces;
using SmartSolutionsLab.Yumney.MealPlan.Domain.WeeklyPlan;
using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shared.CQRS;
using SmartSolutionsLab.Yumney.Shared.Outcomes;
using SmartSolutionsLab.Yumney.Shared.Quantities;

namespace SmartSolutionsLab.Yumney.MealPlan.Application.Commands.Handlers;

public sealed class GenerateShoppingListCommandHandler(
	IMealPlanReadModelRepository readModel,
	IRecipeIngredientLookup recipeIngredients,
	IStaplesProvider staplesProvider,
	IShoppingListWriter shoppingListWriter,
	ICurrentUser currentUser)
	: ICommandHandler<GenerateShoppingListCommand, Result<GenerateShoppingListResultDto>>
{
#pragma warning disable SA1303
	private const string mealPlanSource = "meal-plan";
#pragma warning restore SA1303

	public async Task<Result<GenerateShoppingListResultDto>> HandleAsync(
		GenerateShoppingListCommand command,
		CancellationToken cancellationToken = default)
	{
		var week = command.Week;
		var owner = currentUser.AsOwner();

		var planned = await readModel.GetPlannedRecipesAsync(owner, week, cancellationToken);
		if (planned.Recipes.Count == 0) return GenerateShoppingListErrors.NoRecipes;

		var mergeInputs = await BuildMergeInputsAsync(planned.Recipes, cancellationToken);
		var merged = IngredientLineMerger.Merge(mergeInputs);
		(int staplesSkipped, List<ShoppingItemRequest> itemsToAdd) = await FilterOutStaplesAsync(owner, merged, cancellationToken);

		return new GenerateShoppingListResultDto(itemsToAdd.Count, staplesSkipped);
	}

	private async Task<List<IngredientLineMergeInput>> BuildMergeInputsAsync(
		IReadOnlyList<PlannedRecipeDto> recipes,
		CancellationToken cancellationToken)
	{
		List<IngredientLineMergeInput> inputs = new(recipes.Count);
		foreach (var recipe in recipes)
		{
			var recipeRef = SlotRecipeIdentifier.From(recipe.RecipeIdentifier);
			var ingredients = await recipeIngredients.LookupAsync(recipeRef, cancellationToken);
			if (ingredients.Count == 0) continue;

			inputs.Add(new IngredientLineMergeInput(
				[.. ingredients.Select(ingredient => new ScalableIngredientLine(
					ingredient.Name,
					ingredient.Amount,
					ingredient.Unit,
					ingredient.RecipeServings))],
				recipe.Servings));
		}

		return inputs;
	}

	private async Task<(int StaplesSkipped, List<ShoppingItemRequest> ItemsToAdd)> FilterOutStaplesAsync(
		OwnerIdentifier owner,
		IReadOnlyList<MergedIngredientLine> merged,
		CancellationToken cancellationToken = default)
	{
		var staples = await staplesProvider.GetStapleNamesAsync(cancellationToken);
		var staplesSkipped = 0;
		List<ShoppingItemRequest> itemsToAdd = [];

		foreach (var item in merged)
		{
			if (staples.Contains(item.Name.ToLowerInvariant()))
			{
				staplesSkipped++;
				continue;
			}

			itemsToAdd.Add(new ShoppingItemRequest(item.Name, item.Amount ?? 0m, item.Unit, mealPlanSource));
		}

		if (itemsToAdd.Count > 0)
		{
			await shoppingListWriter.AddItemsAsync(itemsToAdd, cancellationToken);
		}

		return (staplesSkipped, itemsToAdd);
	}
}
