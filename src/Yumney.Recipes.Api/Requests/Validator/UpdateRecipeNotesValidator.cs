using FluentValidation;
using SmartSolutionsLab.Yumney.Recipes.Domain.Recipe;

namespace SmartSolutionsLab.Yumney.Recipes.Api.Requests.Validator;

public sealed class UpdateRecipeNotesValidator : AbstractValidator<UpdateRecipeNotes>
{
	public UpdateRecipeNotesValidator()
	{
		RuleFor(request => request.Notes)
			.MaximumLength(Notes.MaxLength)
			.When(request => request.Notes is not null);
	}
}
