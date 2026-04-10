namespace SmartSolutionsLab.Yumney.Shopping.Api.Requests;

public sealed record AddManualItemRequest(string Name, decimal? Quantity = null, string? Unit = null);
