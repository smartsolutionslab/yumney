namespace SmartSolutionsLab.Yumney.Recipes.Application.DTOs;

public sealed record ChatActionDto(
	ChatActionType Type,
	string? Route = null,
	Guid? RecipeIdentifier = null);
