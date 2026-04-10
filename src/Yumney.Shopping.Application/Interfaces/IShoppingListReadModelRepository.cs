using SmartSolutionsLab.Yumney.Shopping.Application.DTOs;

namespace SmartSolutionsLab.Yumney.Shopping.Application.Interfaces;

public interface IShoppingListReadModelRepository
{
    Task<MergedShoppingListDto> GetByOwnerAsync(string ownerId, CancellationToken cancellationToken = default);
}
