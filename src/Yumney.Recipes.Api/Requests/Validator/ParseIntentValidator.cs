using FluentValidation;
using SmartSolutionsLab.Yumney.Recipes.Application.DTOs;

namespace SmartSolutionsLab.Yumney.Recipes.Api.Requests.Validator;

public sealed class ParseIntentValidator : AbstractValidator<ParseIntentRequestDto>
{
	public ParseIntentValidator()
	{
		RuleFor(request => request.Message).NotEmpty().WithMessage("Message cannot be empty.");
	}
}
