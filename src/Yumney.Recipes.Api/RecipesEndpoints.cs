using System.Text;
using System.Text.Json;
using FluentValidation;
using SmartSolutionsLab.Yumney.Recipes.Api.Requests;
using SmartSolutionsLab.Yumney.Recipes.Application.Commands;
using SmartSolutionsLab.Yumney.Recipes.Application.DTOs;
using SmartSolutionsLab.Yumney.Recipes.Application.Interfaces;
using SmartSolutionsLab.Yumney.Recipes.Application.Queries;
using SmartSolutionsLab.Yumney.Recipes.Domain.Recipe;
using SmartSolutionsLab.Yumney.Recipes.Extraction;
using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shared.CQRS;
using SmartSolutionsLab.Yumney.Shared.Guards;
using SmartSolutionsLab.Yumney.Shared.Web;
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

        group.MapGet("/import/stream", ImportStreamAsync)
            .WithName("ImportRecipeStream")
            .WithTags("Recipes")
            .Produces(StatusCodes.Status200OK, contentType: "text/event-stream")
            .ProducesProblem(StatusCodes.Status502BadGateway)
            .RequireRateLimiting("RecipeImport");

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

    internal static string CompactJson(string json)
    {
        using var document = JsonDocument.Parse(json);
        return JsonSerializer.Serialize(document.RootElement);
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
        var sortField = default(RecipeSortField).ParseNullable(sortBy) ?? RecipeSortField.Date;

        var query = new GetRecipesQuery(
            PagingOptions.Of(Page.From(page), PageSize.From(pageSize)),
            new SortingOptions<RecipeSortField>(sortField, sortDirection),
            SearchTerm.FromNullable(search));

        var result = await handler.HandleAsync(query, cancellationToken);
        return result.ToOk();
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
            RecipeTitle.From(request.Title),
            request.Ingredients.Select(i => i.ToCommandItem()).ToList(),
            request.Steps.Select(s => s.ToCommandItem()).ToList(),
            RecipeDescription.FromNullable(request.Description),
            Servings.FromNullable(request.Servings),
            PreparationTime.FromNullable(request.PrepTimeMinutes),
            CookingTime.FromNullable(request.CookTimeMinutes),
            Difficulty.FromNullable(request.Difficulty),
            ImageUrl.FromNullable(request.ImageUrl),
            RecipeLanguage.FromNullable(request.Language),
            RecipeUrl.FromNullable(request.SourceUrl),
            request.Tags?.Select(t => RecipeTag.From(t)).ToList());
        var result = await handler.HandleAsync(command, cancellationToken);
        return result.ToCreated($"/api/v1/recipes/{result.Value?.Identifier}");
    }

    private static async Task<IResult> GetByIdAsync(
        Guid identifier,
        IQueryHandler<GetRecipeByIdQuery, Result<RecipeDetailDto>> handler,
        CancellationToken cancellationToken)
    {
        var query = new GetRecipeByIdQuery(RecipeIdentifier.From(identifier));
        var result = await handler.HandleAsync(query, cancellationToken);
        return result.ToOk();
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

        var (title, description, ingredients, steps, servings, prepTimeMinutes, cookTimeMinutes, difficulty, imageUrl,
            tags) = request;

        var command = new UpdateRecipeCommand(
            RecipeIdentifier.From(identifier),
            RecipeTitle.From(title),
            ingredients.Select(i => i.ToCommandItem()).ToList(),
            steps.Select(s => s.ToCommandItem()).ToList(),
            RecipeDescription.FromNullable(description),
            Servings.FromNullable(servings),
            PreparationTime.FromNullable(prepTimeMinutes),
            CookingTime.FromNullable(cookTimeMinutes),
            Difficulty.FromNullable(difficulty),
            ImageUrl.FromNullable(imageUrl),
            tags?.Select(t => RecipeTag.From(t)).ToList());
        var result = await handler.HandleAsync(command, cancellationToken);
        return result.ToOk();
    }

    private static async Task<IResult> DeleteAsync(
        Guid identifier,
        ICommandHandler<DeleteRecipeCommand, Result> handler,
        CancellationToken cancellationToken)
    {
        var command = new DeleteRecipeCommand(RecipeIdentifier.From(identifier));
        var result = await handler.HandleAsync(command, cancellationToken);
        return result.ToNoContent();
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

        var command = new ImportRecipeCommand(RecipeUrl.From(request.Url));
        var result = await handler.HandleAsync(command, cancellationToken);
        return result.ToOk();
    }

    private const long MaxPhotoSizeBytes = 10 * 1024 * 1024;

    private static async Task<IResult> ImportFromPhotosAsync(
        IFormFileCollection photos,
        ICommandHandler<ImportRecipeFromPhotosCommand, Result<ExtractedRecipeDto>> handler,
        CancellationToken cancellationToken)
    {
        List<PhotoData> photoDataList = [];

        foreach (var file in photos)
        {
            if (file.Length > MaxPhotoSizeBytes)
            {
                return Results.Problem(
                    statusCode: StatusCodes.Status413PayloadTooLarge,
                    detail: $"Photo '{file.FileName}' exceeds the maximum size of 10 MB.");
            }

            using var memoryStream = new MemoryStream((int)file.Length);
            await file.CopyToAsync(memoryStream, cancellationToken);
            photoDataList.Add(new PhotoData(memoryStream.ToArray(), file.ContentType, file.FileName));
        }

        var command = new ImportRecipeFromPhotosCommand(photoDataList);
        var result = await handler.HandleAsync(command, cancellationToken);
        return result.ToOk();
    }

#pragma warning disable SA1303
    private const int maxStreamBufferLength = 100_000;
#pragma warning restore SA1303

    private static async Task ImportStreamAsync(
        HttpContext httpContext,
        string url,
        IWebScraper scraper,
        IRecipeExtractionService extraction,
        CancellationToken cancellationToken)
    {
        httpContext.Response.ContentType = "text/event-stream";
        httpContext.Response.Headers.CacheControl = "no-cache";
        httpContext.Response.Headers.Connection = "keep-alive";

        async Task WriteSseEventAsync(string eventType, string data)
        {
            var line = $"event: {eventType}\ndata: {data}\n\n";
            await httpContext.Response.WriteAsync(line, cancellationToken);
            await httpContext.Response.Body.FlushAsync(cancellationToken);
        }

        RecipeUrl recipeUrl;
        try
        {
            recipeUrl = RecipeUrl.From(url);
        }
        catch (GuardException)
        {
            await WriteSseEventAsync("fail", "Invalid URL");
            return;
        }

        await WriteSseEventAsync("status", "Fetching page...");

        var scrapeResult = await scraper.ScrapeAsync(recipeUrl, cancellationToken);
        if (scrapeResult.IsFailure)
        {
            await WriteSseEventAsync("fail", scrapeResult.Error!.Message);
            return;
        }

        await WriteSseEventAsync("status", "Extracting recipe...");

        var buffer = new StringBuilder();
        try
        {
            await foreach (var chunk in extraction.StreamExtractAsync(scrapeResult.Value, cancellationToken))
            {
                if (buffer.Length + chunk.Length > maxStreamBufferLength)
                {
                    await WriteSseEventAsync("fail", "Response too large");
                    return;
                }

                buffer.Append(chunk);
                await WriteSseEventAsync("chunk", chunk);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return;
        }
        catch (Exception)
        {
            await WriteSseEventAsync("fail", "Extraction failed");
            return;
        }

        var json = CompactJson(LlmResponseParser.ExtractJson(buffer.ToString()));
        await WriteSseEventAsync("done", json);
    }
}
