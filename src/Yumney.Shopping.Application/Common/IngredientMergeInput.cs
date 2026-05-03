using SmartSolutionsLab.Yumney.Shopping.Application.DTOs;
using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingList;

namespace SmartSolutionsLab.Yumney.Shopping.Application.Common;

public sealed record IngredientMergeInput(
	IReadOnlyList<RecipeIngredientLookupResult> Ingredients,
	Servings? DesiredServings);
