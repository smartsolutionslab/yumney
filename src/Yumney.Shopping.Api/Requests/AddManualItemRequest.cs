using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingList;

namespace SmartSolutionsLab.Yumney.Shopping.Api.Requests;

public sealed record AddManualItemRequest(string Name, decimal? Quantity = null, string? Unit = null)
{
    public (ItemName ItemName, decimal? Quantity, string? Unit) ToValueObjects() =>
        (ItemName.From(Name.Trim()), Quantity, Unit);
}
