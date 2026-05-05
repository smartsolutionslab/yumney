using System.Text;
using System.Text.Json;
using FluentValidation;
using SmartSolutionsLab.Yumney.Recipes.Application.Commands;
using SmartSolutionsLab.Yumney.Recipes.Application.Common;
using SmartSolutionsLab.Yumney.Recipes.Application.DTOs;
using SmartSolutionsLab.Yumney.Recipes.Application.Interfaces;
using SmartSolutionsLab.Yumney.Recipes.Domain.Recipe;
using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shared.CQRS;
using SmartSolutionsLab.Yumney.Shared.Guards;
using SmartSolutionsLab.Yumney.Shared.Outcomes;
using SmartSolutionsLab.Yumney.Shared.Web;

namespace SmartSolutionsLab.Yumney.Recipes.Api;

#pragma warning disable SA1601 // Partial endpoints class is split for file-size reasons.
public static partial class RecipesEndpoints
#pragma warning restore SA1601
{
	/// <summary>
	/// Server-Sent Event types emitted by the import stream endpoint.
	/// Mirrored on the frontend in <c>libs/shared/api-client/import-stream-event.ts</c>.
	/// </summary>
	private static class SseEvent
	{
		public const string Status = "status";
		public const string Chunk = "chunk";
		public const string Field = "field";
		public const string Done = "done";
		public const string Fail = "fail";
	}

	private static class ImportStreaming
	{
		/// <summary>Hard cap on bytes buffered from the LLM stream before aborting.</summary>
		public const int MaxBufferLength = 100_000;
		public const string InvalidUrlMessage = "Invalid URL";
		public const string FetchingStatusMessage = "Fetching page...";
		public const string ExtractingStatusMessage = "Extracting recipe...";
		public const string ResponseTooLargeMessage = "Response too large";
		public const string ExtractionFailedMessage = "Extraction failed";
	}

	internal static string CompactJson(string json)
	{
		using var document = JsonDocument.Parse(json);
		return JsonSerializer.Serialize(document.RootElement);
	}

	internal static async Task ImportStreamAsync(
		HttpContext httpContext,
		string url,
		IWebScraper scraper,
		IRecipeExtractionService extraction,
		ILogger<Program> logger,
		CancellationToken cancellationToken)
	{
		httpContext.Response.ContentType = MediaTypes.TextEventStream;
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
			await WriteSseEventAsync(SseEvent.Fail, ImportStreaming.InvalidUrlMessage);
			return;
		}

		await WriteSseEventAsync(SseEvent.Status, ImportStreaming.FetchingStatusMessage);

		var scrapeResult = await scraper.ScrapeAsync(recipeUrl, cancellationToken);
		if (scrapeResult.IsFailure)
		{
			await WriteSseEventAsync(SseEvent.Fail, scrapeResult.Error!.Message);
			return;
		}

		await WriteSseEventAsync(SseEvent.Status, ImportStreaming.ExtractingStatusMessage);

		var buffer = new StringBuilder();
		var detector = new StreamingJsonFieldDetector();
		try
		{
			await foreach (var chunk in extraction.StreamExtractAsync(scrapeResult.Value, cancellationToken))
			{
				if (buffer.Length + chunk.Length > ImportStreaming.MaxBufferLength)
				{
					await WriteSseEventAsync(SseEvent.Fail, ImportStreaming.ResponseTooLargeMessage);
					return;
				}

				buffer.Append(chunk);
				await WriteSseEventAsync(SseEvent.Chunk, chunk);

				foreach (var (name, value) in detector.Consume(chunk))
				{
					// The field event is opt-in for the frontend — existing clients
					// that subscribe only to chunk/done are unaffected.
					var payload = JsonSerializer.Serialize(new { field = name, value });
					await WriteSseEventAsync(SseEvent.Field, payload);
				}
			}
		}
		catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
		{
			return;
		}
		catch (Exception ex)
		{
			logger.LogError(ex, "Recipe extraction failed while streaming from {Url}", url);
			await WriteSseEventAsync(SseEvent.Fail, ImportStreaming.ExtractionFailedMessage);
			return;
		}

		var json = CompactJson(LlmResponseParser.ExtractJson(buffer.ToString()));
		await WriteSseEventAsync(SseEvent.Done, json);
	}

	private static void MapImportEndpoints(RouteGroupBuilder group)
	{
		group.MapPost("/import", Import)
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

		static async Task<IResult> Import(
			Requests.ImportRecipe request,
			IValidator<Requests.ImportRecipe> validator,
			ICommandHandler<ImportRecipeCommand, Result<ExtractedRecipeDto>> handler,
			CancellationToken cancellationToken)
		{
			var validation = await validator.ValidateAsync(request, cancellationToken);
			if (validation.HasFailed()) return validation.ToValidationProblem();

			var command = new ImportRecipeCommand(RecipeUrl.From(request.Url));

			var result = await handler.HandleAsync(command, cancellationToken);
			return result.ToOk();
		}

		group.MapPost("/import-from-photos", ImportFromPhotos)
			.WithName("ImportRecipeFromPhotos")
			.WithTags("Recipes")
			.Produces<ExtractedRecipeDto>()
			.ProducesProblem(StatusCodes.Status400BadRequest)
			.ProducesProblem(StatusCodes.Status413PayloadTooLarge)
			.ProducesProblem(StatusCodes.Status429TooManyRequests)
			.ProducesProblem(StatusCodes.Status500InternalServerError)
			.RequireRateLimiting("RecipeImport")
			.DisableAntiforgery();

		static async Task<IResult> ImportFromPhotos(
			IFormFileCollection photos,
			IValidator<PhotoData> validator,
			ICommandHandler<ImportRecipeFromPhotosCommand, Result<ExtractedRecipeDto>> handler,
			CancellationToken cancellationToken)
		{
			if (photos.Count == 0 || photos.Count > PhotoDataValidator.MaxPhotos)
			{
				return Results.ValidationProblem(new Dictionary<string, string[]>
				{
					["Photos"] = [$"Must contain between 1 and {PhotoDataValidator.MaxPhotos} photos."],
				});
			}

			var photoDataList = new List<PhotoData>(photos.Count);
			foreach (var file in photos)
			{
				var photoData = await LoadPhotoDataAsync(file, cancellationToken);
				var validation = await validator.ValidateAsync(photoData, cancellationToken);

				if (validation.HasFailed()) return validation.ToValidationProblem();

				photoDataList.Add(photoData);
			}

			var command = new ImportRecipeFromPhotosCommand(photoDataList);

			var result = await handler.HandleAsync(command, cancellationToken);
			return result.ToOk();
		}

		group.MapPost("/recognize-ingredients", RecognizeIngredients)
			.WithName("RecognizeIngredients")
			.WithTags("Recipes")
			.Produces<RecognizedIngredientsResponseDto>()
			.ProducesProblem(StatusCodes.Status400BadRequest)
			.ProducesProblem(StatusCodes.Status413PayloadTooLarge)
			.ProducesProblem(StatusCodes.Status429TooManyRequests)
			.RequireRateLimiting("RecipeImport")
			.DisableAntiforgery();

		static async Task<IResult> RecognizeIngredients(
			IFormFile photo,
			IValidator<PhotoData> validator,
			ICommandHandler<RecognizeIngredientsCommand, Result<RecognizedIngredientsResponseDto>> handler,
			CancellationToken cancellationToken)
		{
			var photoData = await LoadPhotoDataAsync(photo, cancellationToken);
			var validation = await validator.ValidateAsync(photoData, cancellationToken);
			if (validation.HasFailed()) return validation.ToValidationProblem();

			var command = new RecognizeIngredientsCommand(photoData);

			var result = await handler.HandleAsync(command, cancellationToken);
			return result.ToOk();
		}

		group.MapGet("/import/stream", ImportStreamAsync)
			.WithName("ImportRecipeStream")
			.WithTags("Recipes")
			.Produces(StatusCodes.Status200OK, contentType: MediaTypes.TextEventStream)
			.ProducesProblem(StatusCodes.Status502BadGateway)
			.RequireRateLimiting("RecipeImport");

		static async Task<PhotoData> LoadPhotoDataAsync(IFormFile file, CancellationToken cancellationToken)
		{
			using var memoryStream = new MemoryStream((int)file.Length);
			await file.CopyToAsync(memoryStream, cancellationToken);

			return new PhotoData(memoryStream.ToArray(), file.ContentType, file.FileName);
		}
	}
}
