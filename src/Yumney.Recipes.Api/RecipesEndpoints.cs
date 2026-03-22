using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SmartSolutionsLab.Yumney.Recipes.Api.Requests;
using SmartSolutionsLab.Yumney.Recipes.Application.Commands;
using SmartSolutionsLab.Yumney.Recipes.Application.DTOs;
using SmartSolutionsLab.Yumney.Recipes.Application.Queries;
using SmartSolutionsLab.Yumney.Recipes.Domain.Recipe;
using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shared.CQRS;
using SmartSolutionsLab.Yumney.Shared.Web.Validation;

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
            .ProducesProblem(StatusCodes.Status413PayloadTooLarge)
            .ProducesProblem(StatusCodes.Status429TooManyRequests)
            .ProducesProblem(StatusCodes.Status500InternalServerError)
            .ProducesProblem(StatusCodes.Status502BadGateway)
            .ProducesProblem(StatusCodes.Status504GatewayTimeout)
            .RequireRateLimiting("RecipeImport");

        group.MapPost("/import-from-photos", ImportFromPhotosAsync)
            .WithName("ImportRecipeFromPhotos")
            .WithTags("Recipes")
            .Produces<ExtractedRecipeDto>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status413PayloadTooLarge)
            .ProducesProblem(StatusCodes.Status429TooManyRequests)
            .ProducesProblem(StatusCodes.Status500InternalServerError)
            .RequireRateLimiting("RecipeImport")
            .DisableAntiforgery();

        group.MapPost("/", SaveAsync)
            .WithName("SaveRecipe")
            .WithTags("Recipes")
            .Produces<SavedRecipeDto>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPut("/{identifier:guid}", UpdateAsync)
            .WithName("UpdateRecipe")
            .WithTags("Recipes")
            .Produces<RecipeDetailDto>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapDelete("/{identifier:guid}", DeleteAsync)
            .WithName("DeleteRecipe")
            .WithTags("Recipes")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound);

        return app;
    }

    private static async Task<IResult> GetAllAsync(
        IQueryHandler<GetRecipesQuery, Result<PagedResult<RecipeListItemDto>>> handler,
        int page = PagingOptions.DefaultPage,
        int pageSize = PagingOptions.DefaultPageSize,
        string sortBy = "Date",
        SortDirection sortDirection = SortDirection.Descending,
        string? search = null,
        CancellationToken cancellationToken = default)
    {
        var sortField = Enum.TryParse<RecipeSortField>(sortBy, ignoreCase: true, out var parsed)
            ? parsed
            : RecipeSortField.Date;

        var query = new GetRecipesQuery(
            PagingOptions.Of(Page.From(page), PageSize.From(pageSize)),
            new SortingOptions<RecipeSortField>(sortField, sortDirection),
            SearchTerm.FromNullable(search));

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
        var problem = await validator.ValidateAndProblemAsync(request, cancellationToken);
        if (problem is not null)
        {
            return problem;
        }

        var command = new SaveRecipeCommand(
            new RecipeTitle(request.Title),
            request.Ingredients.Select(i => new SaveRecipeIngredientItem(
                new IngredientName(i.Name),
                Amount.FromNullable(i.Amount),
                Unit.FromNullable(i.Unit))).ToList(),
            request.Steps.Select(s => new SaveRecipeStepItem(
                new StepNumber(s.Number),
                new StepDescription(s.Description))).ToList(),
            RecipeDescription.FromNullable(request.Description),
            Servings.FromNullable(request.Servings),
            PreparationTime.FromNullable(request.PrepTimeMinutes),
            CookingTime.FromNullable(request.CookTimeMinutes),
            Difficulty.FromNullable(request.Difficulty),
            ImageUrl.FromNullable(request.ImageUrl),
            RecipeLanguage.FromNullable(request.Language),
            RecipeUrl.FromNullable(request.SourceUrl));
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
        var query = new GetRecipeByIdQuery(new RecipeIdentifier(identifier));
        var result = await handler.HandleAsync(query, cancellationToken);

        if (result.IsFailure)
        {
            return Results.Problem(result.Error!.Message, statusCode: result.Error.HttpStatusCode);
        }

        return Results.Ok(result.Value);
    }

    private static async Task<IResult> UpdateAsync(
        Guid identifier,
        UpdateRecipeRequest request,
        IValidator<UpdateRecipeRequest> validator,
        ICommandHandler<UpdateRecipeCommand, Result<RecipeDetailDto>> handler,
        CancellationToken cancellationToken)
    {
        var problem = await validator.ValidateAndProblemAsync(request, cancellationToken);
        if (problem is not null)
        {
            return problem;
        }

        var command = new UpdateRecipeCommand(
            new RecipeIdentifier(identifier),
            new RecipeTitle(request.Title),
            request.Ingredients.Select(i => new SaveRecipeIngredientItem(
                new IngredientName(i.Name),
                Amount.FromNullable(i.Amount),
                Unit.FromNullable(i.Unit))).ToList(),
            request.Steps.Select(s => new SaveRecipeStepItem(
                new StepNumber(s.Number),
                new StepDescription(s.Description))).ToList(),
            RecipeDescription.FromNullable(request.Description),
            Servings.FromNullable(request.Servings),
            PreparationTime.FromNullable(request.PrepTimeMinutes),
            CookingTime.FromNullable(request.CookTimeMinutes),
            Difficulty.FromNullable(request.Difficulty),
            ImageUrl.FromNullable(request.ImageUrl));
        var result = await handler.HandleAsync(command, cancellationToken);

        if (result.IsFailure)
        {
            return Results.Problem(result.Error!.Message, statusCode: result.Error.HttpStatusCode);
        }

        return Results.Ok(result.Value);
    }

    private static async Task<IResult> DeleteAsync(
        Guid identifier,
        ICommandHandler<DeleteRecipeCommand, Result> handler,
        CancellationToken cancellationToken)
    {
        var command = new DeleteRecipeCommand(new RecipeIdentifier(identifier));
        var result = await handler.HandleAsync(command, cancellationToken);

        if (result.IsFailure)
        {
            return Results.Problem(result.Error!.Message, statusCode: result.Error.HttpStatusCode);
        }

        return Results.NoContent();
    }

    private static async Task<IResult> ImportAsync(
        ImportRecipeRequest request,
        IValidator<ImportRecipeRequest> validator,
        ICommandHandler<ImportRecipeCommand, Result<ExtractedRecipeDto>> handler,
        CancellationToken cancellationToken)
    {
        var problem = await validator.ValidateAndProblemAsync(request, cancellationToken);
        if (problem is not null)
        {
            return problem;
        }

        var command = new ImportRecipeCommand(new RecipeUrl(request.Url));
        var result = await handler.HandleAsync(command, cancellationToken);

        if (result.IsFailure)
        {
            return Results.Problem(result.Error!.Message, statusCode: result.Error.HttpStatusCode);
        }

        return Results.Ok(result.Value);
    }

    private static async Task<IResult> ImportFromPhotosAsync(
        IFormFileCollection photos,
        ICommandHandler<ImportRecipeFromPhotosCommand, Result<ExtractedRecipeDto>> handler,
        CancellationToken cancellationToken)
    {
        var photoDataList = new List<PhotoData>();

        foreach (var file in photos)
        {
            using var memoryStream = new MemoryStream();
            await file.CopyToAsync(memoryStream, cancellationToken);
            photoDataList.Add(new PhotoData(memoryStream.ToArray(), file.ContentType, file.FileName));
        }

        var command = new ImportRecipeFromPhotosCommand(photoDataList);
        var result = await handler.HandleAsync(command, cancellationToken);

        if (result.IsFailure)
        {
            return Results.Problem(result.Error!.Message, statusCode: result.Error.HttpStatusCode);
        }

        return Results.Ok(result.Value);
    }
}
