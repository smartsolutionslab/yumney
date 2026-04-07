using SmartSolutionsLab.Yumney.Recipes.Application.DTOs;
using SmartSolutionsLab.Yumney.Recipes.Domain.Recipe;
using SmartSolutionsLab.Yumney.Shared.Common;

namespace SmartSolutionsLab.Yumney.Recipes.Application.Interfaces;

public interface IChatService
{
    Task<Result<ChatResponseDto>> ChatAsync(
        string message,
        IReadOnlyList<ChatMessageDto> history,
        OwnerIdentifier owner,
        CancellationToken cancellationToken = default);
}
