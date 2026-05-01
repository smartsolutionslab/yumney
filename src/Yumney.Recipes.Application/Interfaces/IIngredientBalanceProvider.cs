using SmartSolutionsLab.Yumney.Shared.Common;

namespace SmartSolutionsLab.Yumney.Recipes.Application.Interfaces;

public interface IIngredientBalanceProvider
{
	Task<IReadOnlyDictionary<string, Freshness>> GetAvailableIngredientsAsync(CancellationToken cancellationToken = default);
}
