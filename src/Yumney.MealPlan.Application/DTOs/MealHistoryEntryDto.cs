namespace SmartSolutionsLab.Yumney.MealPlan.Application.DTOs;

/// <summary>
/// One row of cooked-meal history (US-331 search result). Only Cooked slots
/// are returned — Planned and Skipped are not part of "what I cooked".
/// </summary>
public sealed record MealHistoryEntryDto(
	Guid? RecipeIdentifier,
	string RecipeTitle,
	string Week,
	string Day,
	string MealType);
