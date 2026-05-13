using SmartSolutionsLab.Yumney.Shopping.Application.DTOs;
using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingList;

namespace SmartSolutionsLab.Yumney.Shopping.Application.Interfaces;

public interface IRecipeIngredientLookup
{
	Task<IReadOnlyList<RecipeIngredientLookupResult>> LookupAsync(RecipeReference recipe, CancellationToken cancellationToken = default);
}
