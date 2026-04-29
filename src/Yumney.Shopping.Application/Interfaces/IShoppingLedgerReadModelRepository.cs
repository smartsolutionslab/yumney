using SmartSolutionsLab.Yumney.Shopping.Application.DTOs;

namespace SmartSolutionsLab.Yumney.Shopping.Application.Interfaces;

/// <summary>
/// Read model repository for the shopping list projection.
/// </summary>
public interface IShoppingLedgerReadModelRepository
{
	/// <summary>
	/// Gets the merged shopping list. By default, hides items bought before today.
	/// </summary>
	/// <param name="ownerId">The owner user identifier.</param>
	/// <param name="includePastBought">If true, includes items bought before today.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>The merged shopping list DTO.</returns>
	Task<MergedShoppingListDto> GetByOwnerAsync(string ownerId, bool includePastBought = false, CancellationToken cancellationToken = default);
}
