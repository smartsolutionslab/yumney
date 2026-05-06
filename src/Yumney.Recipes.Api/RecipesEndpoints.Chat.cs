using SmartSolutionsLab.Yumney.Recipes.Api.Requests;
using SmartSolutionsLab.Yumney.Recipes.Application.Commands;
using SmartSolutionsLab.Yumney.Recipes.Application.DTOs;
using SmartSolutionsLab.Yumney.Recipes.Domain.Chat;
using SmartSolutionsLab.Yumney.Shared.CQRS;
using SmartSolutionsLab.Yumney.Shared.Outcomes;
using SmartSolutionsLab.Yumney.Shared.Web;

namespace SmartSolutionsLab.Yumney.Recipes.Api;

#pragma warning disable SA1601
public static partial class RecipesEndpoints
#pragma warning restore SA1601
{
	private static void MapChatEndpoints(RouteGroupBuilder group)
	{
		group.MapPost("/chat", Chat)
			.WithName("RecipeChat")
			.WithTags("Recipes")
			.WithValidation<ChatRequestDto>()
			.Produces<ChatResponseDto>()
			.ProducesProblem(StatusCodes.Status429TooManyRequests)
			.RequireRateLimiting("RecipeImport");

		static async Task<IResult> Chat(
			ChatRequestDto request,
			ICommandHandler<ChatCommand, Result<ChatResponseDto>> handler,
			CancellationToken cancellationToken)
		{
			var (message, history) = request;

			var command = new ChatCommand(
				ChatMessageContent.From(message),
				history.MapToChatHistoryEntries().ToList());

			var result = await handler.HandleAsync(command, cancellationToken);
			return result.ToOk();
		}

		group.MapPost("/parse-intent", ParseIntent)
			.WithName("ParseIntent")
			.WithTags("Recipes")
			.WithValidation<ParseIntentRequestDto>()
			.Produces<ParsedIntentDto>()
			.ProducesProblem(StatusCodes.Status400BadRequest)
			.RequireRateLimiting("RecipeImport");

		static async Task<IResult> ParseIntent(
			ParseIntentRequestDto request,
			ICommandHandler<ParseIntentCommand, Result<ParsedIntentDto>> handler,
			CancellationToken cancellationToken)
		{
			var command = new ParseIntentCommand(request.Message, request.Context);

			var result = await handler.HandleAsync(command, cancellationToken);
			return result.ToOk();
		}

		group.MapPost("/import-from-text", ImportFromText)
			.WithName("ImportRecipeFromText")
			.WithTags("Recipes")
			.WithValidation<ImportFromTextRequestDto>()
			.Produces<ExtractedRecipeDto>()
			.ProducesProblem(StatusCodes.Status400BadRequest)
			.ProducesProblem(StatusCodes.Status500InternalServerError)
			.RequireRateLimiting("RecipeImport");

		static async Task<IResult> ImportFromText(
			ImportFromTextRequestDto request,
			ICommandHandler<ImportRecipeFromTextCommand, Result<ExtractedRecipeDto>> handler,
			CancellationToken cancellationToken)
		{
			var command = new ImportRecipeFromTextCommand(request.Text);

			var result = await handler.HandleAsync(command, cancellationToken);
			return result.ToOk();
		}
	}
}
