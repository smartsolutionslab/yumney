using FluentValidation;
using SmartSolutionsLab.Yumney.Users.Domain.AppUserProfile;

namespace SmartSolutionsLab.Yumney.Users.Api.Requests;

public sealed class UpdateUserProfileValidator : AbstractValidator<UpdateUserProfile>
{
	public UpdateUserProfileValidator()
	{
		RuleFor(request => request.DefaultServings)
			.InclusiveBetween(DefaultServings.MinValue, DefaultServings.MaxValue)
			.WithMessage($"DefaultServings must be between {DefaultServings.MinValue} and {DefaultServings.MaxValue}.");

		RuleFor(request => request.DisplayName!)
			.MaximumLength(DisplayName.MaxLength)
			.When(request => request.DisplayName is not null);

		RuleFor(request => request.PreferredLanguage!)
			.MaximumLength(PreferredLanguage.MaxLength)
			.When(request => request.PreferredLanguage is not null);

		RuleFor(request => request.PreferredUnitSystem!)
			.MaximumLength(PreferredUnitSystem.MaxLength)
			.When(request => request.PreferredUnitSystem is not null);

		RuleFor(request => request.Theme!)
			.MaximumLength(Theme.MaxLength)
			.When(request => request.Theme is not null);

		RuleFor(request => request.CookingEffort!)
			.MaximumLength(CookingEffortPreference.MaxLength)
			.When(request => request.CookingEffort is not null);

		RuleFor(request => request.MinVeggieMeals!.Value)
			.InclusiveBetween(WeeklyBalanceGoals.MinMeals, WeeklyBalanceGoals.MaxMeals)
			.When(request => request.MinVeggieMeals.HasValue);

		RuleFor(request => request.MaxRedMeatMeals!.Value)
			.InclusiveBetween(WeeklyBalanceGoals.MinMeals, WeeklyBalanceGoals.MaxMeals)
			.When(request => request.MaxRedMeatMeals.HasValue);
	}
}
