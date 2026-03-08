using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Yumney.Recipes.Application.Commands;
using Yumney.Recipes.Domain.Recipe;
using Yumney.Shared.Common;
using Yumney.Shared.CQRS;

namespace Yumney.Recipes.Api;

public static class RecipesEndpoints
{
    public static IEndpointRouteBuilder MapRecipesEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/recipes");

        group.MapPost("/import", ImportAsync)
            .WithName("ImportRecipe");

        return app;
    }

    private static async Task<IResult> ImportAsync(
        ImportRecipeRequest request,
        IValidator<ImportRecipeRequest> validator,
        ICommandHandler<ImportRecipeCommand, Result<ImportRecipeResultDto>> handler,
        CancellationToken cancellationToken)
    {
        var validationResult = await validator.ValidateAsync(request, cancellationToken);

        if (!validationResult.IsValid)
        {
            return Results.ValidationProblem(validationResult.ToDictionary());
        }

        var command = new ImportRecipeCommand(new RecipeUrl(request.Url));

        var result = await handler.HandleAsync(command, cancellationToken);

        if (result.IsFailure)
        {
            return result.Error switch
            {
                ImportRecipeErrors.InvalidUrl =>
                    Results.Problem("The provided URL is not valid.", statusCode: 400),
                _ =>
                    Results.Problem("Failed to import recipe.", statusCode: 500),
            };
        }

        return Results.Ok(result.Value);
    }
}
