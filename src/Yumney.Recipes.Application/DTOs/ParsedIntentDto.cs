namespace SmartSolutionsLab.Yumney.Recipes.Application.DTOs;

public sealed record ParsedIntentDto(
	string Intent,
	Dictionary<string, string> Entities,
	string? Clarification);
