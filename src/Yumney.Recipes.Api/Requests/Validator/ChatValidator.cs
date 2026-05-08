using FluentValidation;
using SmartSolutionsLab.Yumney.Recipes.Application.DTOs;

namespace SmartSolutionsLab.Yumney.Recipes.Api.Requests.Validator;

public sealed class ChatValidator : AbstractValidator<ChatRequestDto>
{
	public ChatValidator()
	{
		RuleFor(request => request.Message).NotEmpty().WithMessage("Message cannot be empty.");
	}
}
