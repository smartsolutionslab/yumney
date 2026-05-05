using FluentValidation;
using SmartSolutionsLab.Yumney.Recipes.Application.Commands;
using SmartSolutionsLab.Yumney.Recipes.Domain.Recipe;
using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shared.CQRS;
using SmartSolutionsLab.Yumney.Shared.Outcomes;
using SmartSolutionsLab.Yumney.Shared.Web;
using Requests = SmartSolutionsLab.Yumney.Recipes.Api.Requests;

namespace SmartSolutionsLab.Yumney.Recipes.Api;

#pragma warning disable SA1601
public static partial class RecipesEndpoints
#pragma warning restore SA1601
{
	private static void MapRatingAndNotesEndpoints(RouteGroupBuilder group)
	{
		group.MapPost("/{identifier:guid}/rating", RateRecipeAsync)
			.WithName("RateRecipe")
			.WithTags("Recipes")
			.Produces(StatusCodes.Status204NoContent)
			.ProducesValidationProblem()
			.ProducesProblem(StatusCodes.Status404NotFound);

		group.MapPut("/{identifier:guid}/notes", UpdateRecipeNotesAsync)
			.WithName("UpdateRecipeNotes")
			.WithTags("Recipes")
			.Produces(StatusCodes.Status204NoContent)
			.ProducesValidationProblem()
			.ProducesProblem(StatusCodes.Status404NotFound);
	}

	private static async Task<IResult> RateRecipeAsync(
		Guid identifier,
		Requests.RateRecipeRequest request,
		IValidator<Requests.RateRecipeRequest> validator,
		ICommandHandler<RateRecipeCommand, Result> handler,
		CancellationToken cancellationToken)
	{
		var validation = await validator.ValidateAsync(request, cancellationToken);
		if (validation.HasFailed()) return validation.ToValidationProblem();

		var command = new RateRecipeCommand(RecipeIdentifier.From(identifier), Rating.From(request.Rating));
		var result = await handler.HandleAsync(command, cancellationToken);
		return result.ToNoContent();
	}

	private static async Task<IResult> UpdateRecipeNotesAsync(
		Guid identifier,
		Requests.UpdateRecipeNotesRequest request,
		IValidator<Requests.UpdateRecipeNotesRequest> validator,
		ICommandHandler<UpdateRecipeNotesCommand, Result> handler,
		CancellationToken cancellationToken)
	{
		var validation = await validator.ValidateAsync(request, cancellationToken);
		if (validation.HasFailed()) return validation.ToValidationProblem();

		var command = new UpdateRecipeNotesCommand(
			RecipeIdentifier.From(identifier),
			Notes.FromNullable(request.Notes));
		var result = await handler.HandleAsync(command, cancellationToken);
		return result.ToNoContent();
	}
}
