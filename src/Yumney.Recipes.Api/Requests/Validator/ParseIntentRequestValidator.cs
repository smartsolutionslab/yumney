using FluentValidation;
using SmartSolutionsLab.Yumney.Recipes.Application.DTOs;

namespace SmartSolutionsLab.Yumney.Recipes.Api.Requests.Validator;

public sealed class ParseIntentRequestValidator : AbstractValidator<ParseIntentRequestDto>
{
    public ParseIntentRequestValidator()
    {
        RuleFor(x => x.Message).NotEmpty().WithMessage("Message cannot be empty.");
    }
}
