using SmartSolutionsLab.Yumney.Shopping.Application.DTOs;

namespace SmartSolutionsLab.Yumney.Shopping.Application.Interfaces;

/// <summary>
/// Read access to the ingredient balance projection. Returns ledger-derived
/// at-home rows (Bought − Consumed − Removed) only; the query handler combines
/// these with the user's staples to produce the full balance sheet.
/// </summary>
public interface IIngredientBalanceReadModelRepository
{
	Task<IReadOnlyList<IngredientBalanceItemDto>> GetAtHomeItemsAsync(string ownerId, CancellationToken cancellationToken = default);
}
