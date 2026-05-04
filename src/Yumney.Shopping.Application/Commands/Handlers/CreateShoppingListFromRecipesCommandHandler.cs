using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shared.CQRS;
using SmartSolutionsLab.Yumney.Shared.Outcomes;
using SmartSolutionsLab.Yumney.Shopping.Application.Common;
using SmartSolutionsLab.Yumney.Shopping.Application.DTOs;
using SmartSolutionsLab.Yumney.Shopping.Application.Interfaces;
using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingList;

namespace SmartSolutionsLab.Yumney.Shopping.Application.Commands.Handlers;

public sealed class CreateShoppingListFromRecipesCommandHandler(
	IRecipeIngredientLookup recipeLookup,
	IShoppingListEventStore eventStore,
	ICurrentUser currentUser)
	: ICommandHandler<CreateShoppingListFromRecipesCommand, Result<ShoppingListDetailDto>>
{
	public async Task<Result<ShoppingListDetailDto>> HandleAsync(
		CreateShoppingListFromRecipesCommand command,
		CancellationToken cancellationToken = default)
	{
		var (title, recipes) = command;
		if (recipes.Count == 0)
		{
			return Result<ShoppingListDetailDto>.Failure(CreateShoppingListFromRecipesErrors.NoRecipesProvided);
		}

		var inputs = new List<IngredientMergeInput>(recipes.Count);
		foreach (var selection in recipes)
		{
			var ingredients = await recipeLookup.LookupAsync(selection.Recipe, cancellationToken);
			if (ingredients.Count == 0) continue;
			inputs.Add(new IngredientMergeInput(ingredients, selection.DesiredServings));
		}

		if (inputs.Count == 0)
		{
			return Result<ShoppingListDetailDto>.Failure(CreateShoppingListFromRecipesErrors.NoIngredientsResolved);
		}

		var merged = IngredientMerger.Merge(inputs);
		var items = merged
			.Select(merge => Domain.ShoppingList.ShoppingListItem.Create(merge.Name, merge.Quantity))
			.ToList();

		var owner = currentUser.AsOwner();
		var shoppingList = ShoppingList.Create(title, owner, items, recipeReference: null);

		await eventStore.SaveAsync(shoppingList, cancellationToken);

		return shoppingList.ToDetailDto();
	}
}
