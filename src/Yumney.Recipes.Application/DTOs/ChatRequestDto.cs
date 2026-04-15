namespace SmartSolutionsLab.Yumney.Recipes.Application.DTOs;

public sealed record ChatRequestDto(string Message, IReadOnlyList<ChatMessageDto> History);
