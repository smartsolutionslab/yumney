using SmartSolutionsLab.Yumney.Recipes.Application.DTOs;
using SmartSolutionsLab.Yumney.Recipes.Application.Interfaces;
using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shared.CQRS;

namespace SmartSolutionsLab.Yumney.Recipes.Application.Commands.Handlers;

public sealed class ChatCommandHandler(
	IChatService chatService,
	ICurrentUser currentUser) : ICommandHandler<ChatCommand, Result<ChatResponseDto>>
{
	public async Task<Result<ChatResponseDto>> HandleAsync(ChatCommand command, CancellationToken cancellationToken = default)
	{
		var (message, history) = command;
		var owner = currentUser.AsOwner();

		return await chatService.ChatAsync(message, history, owner, cancellationToken);
	}
}
