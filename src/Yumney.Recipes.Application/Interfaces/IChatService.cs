using SmartSolutionsLab.Yumney.Recipes.Application.DTOs;
using SmartSolutionsLab.Yumney.Recipes.Domain.Chat;
using SmartSolutionsLab.Yumney.Recipes.Domain.Recipe;
using SmartSolutionsLab.Yumney.Shared.Outcomes;

namespace SmartSolutionsLab.Yumney.Recipes.Application.Interfaces;

public interface IChatService
{
	Task<Result<ChatResponseDto>> ChatAsync(
		ChatMessageContent message,
		IReadOnlyList<ChatHistoryEntry> history,
		OwnerIdentifier owner,
		CancellationToken cancellationToken = default);
}
