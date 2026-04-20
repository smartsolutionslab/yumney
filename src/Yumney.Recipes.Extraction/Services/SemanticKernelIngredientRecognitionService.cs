using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using SmartSolutionsLab.Yumney.Recipes.Application.Commands;
using SmartSolutionsLab.Yumney.Recipes.Application.Common;
using SmartSolutionsLab.Yumney.Recipes.Application.DTOs;
using SmartSolutionsLab.Yumney.Recipes.Application.Interfaces;
using SmartSolutionsLab.Yumney.Shared.Common;

namespace SmartSolutionsLab.Yumney.Recipes.Extraction.Services;

#pragma warning disable SA1601
#pragma warning disable SA1311
public sealed partial class SemanticKernelIngredientRecognitionService(Kernel kernel, ILogger<SemanticKernelIngredientRecognitionService> logger)
	: IIngredientRecognitionService
{
	private static readonly JsonSerializerOptions jsonOptions = new()
	{
		PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
		DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
	};

	public async Task<Result<RecognizedIngredientsResponseDto>> RecognizeAsync(PhotoData photo, CancellationToken cancellationToken = default)
	{
		using var activity = ExtractionDiagnostics.ActivitySource.StartActivity("recognize.ingredients");
		activity?.SetTag("recognize.photo_size", photo.Content.Length);

		var chatHistory = new ChatHistory();
		chatHistory.AddSystemMessage(ExtractionPrompts.IngredientRecognition);
		ChatMessageContentItemCollection messageItems =
		[
			new TextContent("Identify the food ingredients in this image:"),
			new ImageContent(photo.Content, photo.ContentType),
		];
		chatHistory.AddUserMessage(messageItems);

		var chatCompletion = kernel.GetRequiredService<IChatCompletionService>();
		string response;
		try
		{
			var result = await chatCompletion.GetChatMessageContentAsync(chatHistory, cancellationToken: cancellationToken);
			response = result.Content ?? string.Empty;

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
			var json = LlmResponseParser.ExtractJson(response);
			var parsed = JsonSerializer.Deserialize<RecognizedIngredientsResponseDto>(json, jsonOptions);
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
