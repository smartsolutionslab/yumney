using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SmartSolutionsLab.Yumney.Shared.Common;

public interface IShoppingListWriter
{
	Task AddItemsAsync(string ownerId, IReadOnlyList<ShoppingItemRequest> items, CancellationToken cancellationToken = default);
}
