using SmartSolutionsLab.Yumney.Shared.Quantities;

namespace SmartSolutionsLab.Yumney.Recipes.Application.Interfaces;

public interface IIngredientBalanceProvider
{
	Task<IReadOnlyDictionary<string, Freshness>> GetAvailableIngredientsAsync(CancellationToken cancellationToken = default);
}
