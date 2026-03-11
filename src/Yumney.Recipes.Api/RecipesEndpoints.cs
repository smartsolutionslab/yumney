using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SmartSolutionsLab.Yumney.Recipes.Application.Commands;
using SmartSolutionsLab.Yumney.Recipes.Application.DTOs;
using SmartSolutionsLab.Yumney.Recipes.Application.Queries;
using SmartSolutionsLab.Yumney.Recipes.Domain.Recipe;
using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shared.CQRS;

namespace SmartSolutionsLab.Yumney.Recipes.Api;

public static class RecipesEndpoints
{
    public static IEndpointRouteBuilder MapRecipesEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/recipes");

        group.MapGet("/", GetAllAsync)
            .WithName("GetRecipes")
            .WithTags("Recipes")
            .Produces<PagedResult<RecipeListItemDto>>();

        group.MapGet("/{identifier:guid}", GetByIdAsync)
            .WithName("GetRecipeById")
            .WithTags("Recipes")
            .Produces<RecipeDetailDto>()
            .ProducesProblem(StatusCodes.Status404NotFound);

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

    private static async Task<IResult> GetAllAsync(
        IQueryHandler<GetRecipesQuery, Result<PagedResult<RecipeListItemDto>>> handler,
        int page = 1,
        int pageSize = 20,
        RecipeSortField sortBy = RecipeSortField.Date,
        SortDirection sortDirection = SortDirection.Descending,
        CancellationToken cancellationToken = default)
    {
        var query = GetRecipesQuery.FromRequest(page, pageSize, sortBy, sortDirection);
        var result = await handler.HandleAsync(query, cancellationToken);

        if (result.IsFailure)
        {
            return Results.Problem(result.Error!.Message, statusCode: result.Error.HttpStatusCode);
        }

        return Results.Ok(result.Value);
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

        var command = SaveRecipeCommand.FromRequest(request);
        var result = await handler.HandleAsync(command, cancellationToken);

        if (result.IsFailure)
        {
            return Results.Problem(result.Error!.Message, statusCode: result.Error.HttpStatusCode);
        }

        return Results.Created($"/api/v1/recipes/{result.Value.Identifier}", result.Value);
    }

    private static async Task<IResult> GetByIdAsync(
        Guid identifier,
        IQueryHandler<GetRecipeByIdQuery, Result<RecipeDetailDto>> handler,
        CancellationToken cancellationToken)
    {
        var query = GetRecipeByIdQuery.FromRequest(identifier);
        var result = await handler.HandleAsync(query, cancellationToken);

        if (result.IsFailure)
        {
            return Results.Problem(result.Error!.Message, statusCode: result.Error.HttpStatusCode);
        }

        return Results.Ok(result.Value);
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

        var command = ImportRecipeCommand.FromRequest(request.Url);
        var result = await handler.HandleAsync(command, cancellationToken);

        if (result.IsFailure)
        {
            return Results.Problem(result.Error!.Message, statusCode: result.Error.HttpStatusCode);
        }

        return Results.Ok(result.Value);
    }
}
