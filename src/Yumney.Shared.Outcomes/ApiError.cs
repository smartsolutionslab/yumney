namespace SmartSolutionsLab.Yumney.Shared.Outcomes;

public sealed record ApiError(string Code, string Message, int HttpStatusCode);
