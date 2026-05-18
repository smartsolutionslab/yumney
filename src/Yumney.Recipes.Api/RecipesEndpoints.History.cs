using SmartSolutionsLab.Yumney.Recipes.Application.Commands;
using SmartSolutionsLab.Yumney.Recipes.Domain.Recipe;
using SmartSolutionsLab.Yumney.Shared.CQRS;
using SmartSolutionsLab.Yumney.Shared.Outcomes;
using SmartSolutionsLab.Yumney.Shared.Web;

namespace SmartSolutionsLab.Yumney.Recipes.Api;

#pragma warning disable SA1601
public static partial class RecipesEndpoints
#pragma warning restore SA1601
{
	private static void MapHistoryEndpoints(RouteGroupBuilder group)
	{
		group.MapPost("/{identifier:guid}/cooked", TrackCookedAsync)
			.WithName("TrackRecipeCooked")
			.WithTags("Recipes")
			.Produces(StatusCodes.Status204NoContent)
			.ProducesProblem(StatusCodes.Status404NotFound);
	}

	private static async Task<IResult> TrackCookedAsync(Guid identifier, ICommandHandler<TrackRecipeCookedCommand, Result> handler, CancellationToken cancellationToken)
	{
		var command = new TrackRecipeCookedCommand(RecipeIdentifier.From(identifier));
		var result = await handler.HandleAsync(command, cancellationToken);
		return result.ToNoContent();
	}
}
