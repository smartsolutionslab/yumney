namespace SmartSolutionsLab.Yumney.Users.Application.DTOs;

public sealed record UserActivityPageDto(IReadOnlyList<UserActivityDto> Items, string? NextCursor);
