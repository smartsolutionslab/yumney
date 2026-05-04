using SmartSolutionsLab.Yumney.Shared.CQRS;
using SmartSolutionsLab.Yumney.Shared.Outcomes;
using SmartSolutionsLab.Yumney.Users.Application.DTOs;

namespace SmartSolutionsLab.Yumney.Users.Application.Commands;

public sealed record UpdateUserProfileCommand(
	int DefaultServings,
	string? DietaryType,
	IReadOnlyList<string> Restrictions,
	int? MinVeggieMeals,
	int? MaxRedMeatMeals,
	string? CookingEffort) : ICommand<Result<UserProfileDto>>;
