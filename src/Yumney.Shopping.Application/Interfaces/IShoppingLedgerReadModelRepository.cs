using SmartSolutionsLab.Yumney.Shopping.Application.DTOs;
using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingList;

namespace SmartSolutionsLab.Yumney.Shopping.Application.Interfaces;

public interface IShoppingLedgerReadModelRepository
{
	Task<MergedShoppingListDto> GetByOwnerAsync(OwnerIdentifier owner, bool includePastBought = false, CancellationToken cancellationToken = default);
}
