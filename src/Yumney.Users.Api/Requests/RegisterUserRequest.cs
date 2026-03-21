namespace SmartSolutionsLab.Yumney.Users.Api.Requests;

public sealed record RegisterUserRequest(string Email, string Password, string DisplayName);
