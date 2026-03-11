namespace SmartSolutionsLab.Yumney.Shared.Common;

public sealed record ApiError(string Code, string Message, int HttpStatusCode);
