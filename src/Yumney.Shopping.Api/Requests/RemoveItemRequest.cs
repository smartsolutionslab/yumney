namespace SmartSolutionsLab.Yumney.Shopping.Api.Requests;

public sealed record RemoveItemRequest(string Name, decimal? Quantity = null, string? Unit = null, string? Reason = null);
