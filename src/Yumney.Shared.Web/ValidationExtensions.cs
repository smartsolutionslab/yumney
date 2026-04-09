using FluentValidation.Results;
using Microsoft.AspNetCore.Http;

namespace SmartSolutionsLab.Yumney.Shared.Web;

public static class ValidationExtensions
{
    public static bool HasFailed(this ValidationResult result) => !result.IsValid;

    public static IResult ToValidationProblem(this ValidationResult result)
        => Results.ValidationProblem(result.ToDictionary());
}
