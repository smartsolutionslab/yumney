using System.Diagnostics;
using System.Text.Json;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SmartSolutionsLab.Yumney.Recipes.Application.Commands;
using SmartSolutionsLab.Yumney.Recipes.Application.DTOs;
using SmartSolutionsLab.Yumney.Recipes.Application.Interfaces;
using SmartSolutionsLab.Yumney.Shared.AI;
using SmartSolutionsLab.Yumney.Shared.Outcomes;

namespace SmartSolutionsLab.Yumney.Recipes.Extraction.Services;

#pragma warning disable SA1601
public sealed partial class LlmIngredientRecognitionService(
	[FromKeyedServices(LlmTasks.Vision)] IChatClient chatClient,
	ILogger<LlmIngredientRecognitionService> logger)
	: IIngredientRecognitionService
{
	public async Task<Result<RecognizedIngredientsResponseDto>> RecognizeAsync(PhotoData photo, CancellationToken cancellationToken = default)
	{
		using var activity = ExtractionDiagnostics.ActivitySource.StartActivity("recognize.ingredients");
		activity?.SetTag("recognize.photo_size", photo.Content.Length);

		List<ChatMessage> messages =
		[
			new(ChatRole.System, ExtractionPrompts.IngredientRecognition),
			new(ChatRole.User, [new TextContent("Identify the food ingredients in this image:"), new DataContent(photo.Content, photo.ContentType)]),
		];

		string response;
		try
		{
			var result = await chatClient.GetResponseAsync(
				messages,
				LlmJson.OptionsFor<RecognizedIngredientsResponseDto>(),
				cancellationToken);
			response = result.Text;

			activity?.SetTag("llm.response_length", response.Length);
			activity?.SetTag("llm.model", result.ModelId);
		}
		catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
		{
			throw;
		}
		catch (Exception ex)
		{
			activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
			LogLlmCallFailed(ex.Message);
			return ImportRecipeErrors.ExtractionFailed;
		}

		return ParseResponse(response);
	}

	private Result<RecognizedIngredientsResponseDto> ParseResponse(string response)
	{
		try
		{
			var parsed = LlmJson.Deserialize<RecognizedIngredientsResponseDto>(response);
			if (parsed is null)
			{
				LogParsingFailed("Deserialization returned null");
				return ImportRecipeErrors.ExtractionFailed;
			}

			return parsed;
		}
		catch (JsonException ex)
		{
			LogParsingFailed(ex.Message);
			return ImportRecipeErrors.ExtractionFailed;
		}
	}

	[LoggerMessage(Level = LogLevel.Warning, Message = "Ingredient recognition LLM call failed: {Reason}")]
	private partial void LogLlmCallFailed(string reason);

	[LoggerMessage(Level = LogLevel.Warning, Message = "Ingredient recognition response parsing failed: {Reason}")]
	private partial void LogParsingFailed(string reason);
}
