using System.Text;
using System.Text.Json;
using FluentValidation;
using SmartSolutionsLab.Yumney.Recipes.Api.Requests;
using SmartSolutionsLab.Yumney.Recipes.Application.Commands;
using SmartSolutionsLab.Yumney.Recipes.Application.DTOs;
using SmartSolutionsLab.Yumney.Recipes.Application.Interfaces;
using SmartSolutionsLab.Yumney.Recipes.Application.Queries;
using SmartSolutionsLab.Yumney.Recipes.Domain.Chat;
using SmartSolutionsLab.Yumney.Recipes.Domain.Recipe;
using SmartSolutionsLab.Yumney.Recipes.Extraction.Services;
using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shared.CQRS;
using SmartSolutionsLab.Yumney.Shared.Guards;
using SmartSolutionsLab.Yumney.Shared.Web;
using SmartSolutionsLab.Yumney.Shared.Web.Validation;

namespace SmartSolutionsLab.Yumney.Recipes.Api;

#pragma warning disable SA1601 // Partial endpoints class is split for file-size reasons; documentation lives on individual methods.
public static partial class RecipesEndpoints
#pragma warning restore SA1601
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

        group.MapPost("/recognize-ingredients", RecognizeIngredientsAsync)
            .WithName("RecognizeIngredients")
            .WithTags("Recipes")
            .Produces<RecognizedIngredientsResponseDto>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status413PayloadTooLarge)
            .ProducesProblem(StatusCodes.Status429TooManyRequests)
            .RequireRateLimiting("RecipeImport")
            .DisableAntiforgery();

        group.MapPost("/chat", ChatAsync)
            .WithName("RecipeChat")
            .WithTags("Recipes")
            .Produces<ChatResponseDto>()
            .ProducesProblem(StatusCodes.Status429TooManyRequests)
            .RequireRateLimiting("RecipeImport");

        group.MapGet("/import/stream", ImportStreamAsync)
            .WithName("ImportRecipeStream")
            .WithTags("Recipes")
            .Produces(StatusCodes.Status200OK, contentType: MediaTypes.TextEventStream)
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
        string? tags = null,
        string? difficulty = null,
        int? maxPrepTime = null,
        int? maxCookTime = null,
        CancellationToken cancellationToken = default)
    {
        var query = new GetRecipesQuery(
            PagingOptions.From(page, pageSize),
            SortingOptions<RecipeSortField>.Parse(sortBy, sortDirection, RecipeSortField.Date),
            SearchTerm.FromNullable(search),
            RecipeFilterParser.Build(tags, difficulty, maxPrepTime, maxCookTime));

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
        if (problem is not null) return problem;

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
        if (problem is not null) return problem;

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
        if (problem is not null) return problem;

        var command = new ImportRecipeCommand(RecipeUrl.From(request.Url));
        var result = await handler.HandleAsync(command, cancellationToken);
        return result.ToOk();
    }

    private static async Task<IResult> ImportFromPhotosAsync(
        IFormFileCollection photos,
        ICommandHandler<ImportRecipeFromPhotosCommand, Result<ExtractedRecipeDto>> handler,
        CancellationToken cancellationToken)
    {
        var oversized = photos.FirstOrDefault(p => p.Length > PhotoValidator.MaxPhotoSizeBytes);
        if (oversized is not null) return PhotoTooLargeProblem(oversized.FileName);

        List<PhotoData> photoDataList = [];
        foreach (var file in photos)
        {
            photoDataList.Add(await LoadPhotoDataAsync(file, cancellationToken));
        }

        var command = new ImportRecipeFromPhotosCommand(photoDataList);
        var result = await handler.HandleAsync(command, cancellationToken);
        return result.ToOk();
    }

    private static async Task<IResult> RecognizeIngredientsAsync(
        IFormFile photo,
        ICommandHandler<RecognizeIngredientsCommand, Result<RecognizedIngredientsResponseDto>> handler,
        CancellationToken cancellationToken)
    {
        if (photo.Length > PhotoValidator.MaxPhotoSizeBytes) return PhotoTooLargeProblem(photo.FileName);

        var photoData = await LoadPhotoDataAsync(photo, cancellationToken);
        var command = new RecognizeIngredientsCommand(photoData);
        var result = await handler.HandleAsync(command, cancellationToken);
        return result.ToOk();
    }

    private static async Task<PhotoData> LoadPhotoDataAsync(IFormFile file, CancellationToken cancellationToken)
    {
        using var memoryStream = new MemoryStream((int)file.Length);
        await file.CopyToAsync(memoryStream, cancellationToken);
        return new PhotoData(memoryStream.ToArray(), file.ContentType, file.FileName);
    }

    private static IResult PhotoTooLargeProblem(string fileName) =>
        Results.Problem(statusCode: StatusCodes.Status413PayloadTooLarge, detail: $"Photo '{fileName}' exceeds the maximum size of 10 MB.");

#pragma warning disable SA1303 // editorconfig requires camelCase for private const fields
    private const string emptyChatMessageError = "Message cannot be empty.";
#pragma warning restore SA1303

    private static async Task<IResult> ChatAsync(
        ChatRequestDto request,
        ICommandHandler<ChatCommand, Result<ChatResponseDto>> handler,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Message))
        {
            return Results.Problem(
                statusCode: StatusCodes.Status400BadRequest,
                detail: emptyChatMessageError);
        }

        var historyEntries = request.History
            .Select(h => new ChatHistoryEntry(ChatRole.From(h.Role), ChatMessageContent.From(h.Content)))
            .ToList();
        var command = new ChatCommand(ChatMessageContent.From(request.Message), historyEntries);
        var result = await handler.HandleAsync(command, cancellationToken);
        return result.ToOk();
    }
}
