using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Yumney.Recipes.Application.Commands;
using Yumney.Recipes.Application.DTOs;
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
            .WithName("ImportRecipe")
            .WithTags("Recipes")
            .Produces<ExtractedRecipeDto>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status500InternalServerError)
            .ProducesProblem(StatusCodes.Status502BadGateway)
            .ProducesProblem(StatusCodes.Status504GatewayTimeout);

        group.MapPost("/", SaveAsync)
            .WithName("SaveRecipe")
            .WithTags("Recipes")
            .Produces<SavedRecipeDto>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status409Conflict);

        return app;
    }

    private static async Task<IResult> SaveAsync(
        SaveRecipeRequest request,
        IValidator<SaveRecipeRequest> validator,
        ICommandHandler<SaveRecipeCommand, Result<SavedRecipeDto>> handler,
        CancellationToken cancellationToken)
    {
        var validationResult = await validator.ValidateAsync(request, cancellationToken);

        if (!validationResult.IsValid)
        {
            return Results.ValidationProblem(validationResult.ToDictionary());
        }

        var command = new SaveRecipeCommand(
            new RecipeTitle(request.Title),
            request.Ingredients.Select(i => new SaveRecipeIngredientCommand(
                new IngredientName(i.Name),
                Amount.FromNullable(i.Amount),
                Unit.FromNullable(i.Unit))).ToList(),
            request.Steps.Select(s => new SaveRecipeStepCommand(
                new StepNumber(s.Number),
                new StepDescription(s.Description))).ToList(),
            RecipeDescription.FromNullable(request.Description),
            Servings.FromNullable(request.Servings),
            PreparationTime.FromNullable(request.PrepTimeMinutes),
            CookingTime.FromNullable(request.CookTimeMinutes),
            Difficulty.FromNullable(request.Difficulty),
            ImageUrl.FromNullable(request.ImageUrl),
            SourceUrl: RecipeUrl.FromNullable(request.SourceUrl));

        var result = await handler.HandleAsync(command, cancellationToken);

        if (result.IsFailure)
        {
            return result.Error switch
            {
                SaveRecipeErrors.AlreadyImported => Results.Problem("This recipe has already been imported.", statusCode: 409),
                _ => Results.Problem("Failed to save recipe.", statusCode: 500),
            };
        }

        return Results.Created($"/api/v1/recipes/{result.Value.Identifier}", result.Value);
    }

    private static async Task<IResult> ImportAsync(
        ImportRecipeRequest request,
        IValidator<ImportRecipeRequest> validator,
        ICommandHandler<ImportRecipeCommand, Result<ExtractedRecipeDto>> handler,
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
                ImportRecipeErrors.PageUnreachable => Results.Problem("Could not reach the website.", statusCode: 502),
                ImportRecipeErrors.ScrapeTimeout => Results.Problem("Extraction timed out.", statusCode: 504),
                ImportRecipeErrors.NoRecipeFound => Results.Problem("No recipe found on this page.", statusCode: 404),
                ImportRecipeErrors.ExtractionFailed => Results.Problem("Recipe extraction failed.", statusCode: 500),
                _ => Results.Problem("Failed to import recipe.", statusCode: 500),
            };
        }

        return Results.Ok(result.Value);
    }
}
