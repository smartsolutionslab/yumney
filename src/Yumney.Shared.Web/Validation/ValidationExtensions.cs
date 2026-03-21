using FluentValidation;
using Microsoft.AspNetCore.Http;

namespace SmartSolutionsLab.Yumney.Shared.Web.Validation;

public static class ValidationExtensions
{
    public static async Task<IResult?> ValidateAndProblemAsync<T>(
        this IValidator<T> validator,
        T request,
        CancellationToken cancellationToken = default)
    {
        var result = await validator.ValidateAsync(request, cancellationToken);
        return result.IsValid ? null : Results.ValidationProblem(result.ToDictionary());
    }
}
