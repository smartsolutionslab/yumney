using SmartSolutionsLab.Yumney.Recipes.Domain.Chat;

namespace SmartSolutionsLab.Yumney.Recipes.Application.DTOs;

public sealed record ChatMessageDto(string Role, string Content)
{
	public ChatHistoryEntry ToHistoryEntry() => new(
		ChatRole.From(Role),
		ChatMessageContent.From(Content));
}
