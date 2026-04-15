using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingList;

namespace SmartSolutionsLab.Yumney.Shopping.Api.Requests;

public sealed record RemoveItemRequest(string Name, decimal? Quantity = null, string? Unit = null, string? Reason = null)
{
    public (ItemName ItemName, decimal? Quantity, string? Unit, string? Reason) ToValueObjects() =>
        (ItemName.From(Name.Trim()), Quantity, Unit, Reason);
}
