using SmartSolutionsLab.Yumney.Recipes.Application.DTOs;
using SmartSolutionsLab.Yumney.Recipes.Application.Interfaces;
using SmartSolutionsLab.Yumney.Recipes.Domain.Chat;
using SmartSolutionsLab.Yumney.Recipes.Domain.Recipe;
using SmartSolutionsLab.Yumney.Shared.Outcomes;

namespace SmartSolutionsLab.Yumney.Recipes.Extraction.TestStubs;

internal sealed class StubChatService : IChatService
{
	public Task<Result<ChatResponseDto>> ChatAsync(
		ChatMessageContent message,
		IReadOnlyList<ChatHistoryEntry> history,
		OwnerIdentifier owner,
		CancellationToken cancellationToken = default) =>
		Task.FromResult(Result<ChatResponseDto>.Success(new ChatResponseDto("Stub reply.", [])));
}
