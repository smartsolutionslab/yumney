using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingList;

namespace SmartSolutionsLab.Yumney.Shopping.Application.Interfaces;

public interface IStaplesProvider
{
	Task<IReadOnlySet<string>> GetStapleNamesAsync(OwnerIdentifier owner, CancellationToken cancellationToken = default);
}
