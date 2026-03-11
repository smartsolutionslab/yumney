namespace SmartSolutionsLab.Yumney.Users.Application.Commands;

public sealed record RegisterUserRequest(string Email, string Password, string DisplayName);
